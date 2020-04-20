package cleanup

import (
	"context"
	"fmt"
	"os"
	"strconv"
	"strings"
	"time"

	"golang.org/x/oauth2/google"
	"google.golang.org/api/compute/v1"
)

const TtlLabelName = "ttl"

func getMetadata(instance *compute.Instance, key string) (value string, ok bool) {
	for _, item := range instance.Metadata.Items {
		if item.Key == key {
			return *item.Value, true
		}
	}

	// key not found.
	return "", false
}

func isDueForCleanup(instance *compute.Instance) (due bool, err error) {
	ttlString, ok := getMetadata(instance, TtlLabelName)
	if ok {
		// Parse ttl metadata value
		ttlInMinutes, err := strconv.Atoi(ttlString)
		if err != nil {
			// No such metadata key, that implies that this is not a temporary instance.
			return false, nil
		}

		// Get age of VM
		creationTimestamp, err := time.Parse(time.RFC3339, instance.CreationTimestamp)
		if err != nil {
			return false, fmt.Errorf("Failed to parset creation date of VM: %v", err)
		}

		ageInMinutes := int(time.Since(creationTimestamp).Minutes())
		fmt.Printf("Instance %s was created %d minutes ago, the max age is %d minutes\n",
			instance.Name, ageInMinutes, ttlInMinutes)

		return ttlInMinutes < ageInMinutes, nil
	}

	return false, nil
}

func cleanupInstances(projectId string) error {
	ctx := context.Background()

	client, err := google.DefaultClient(ctx, compute.ComputeScope)
	if err != nil {
		return fmt.Errorf("Failed to create client: %v", err)
	}

	computeService, err := compute.New(client)
	if err != nil {
		return fmt.Errorf("Failed to create service: %v", err)
	}

	aggregatedListCall := computeService.Instances.AggregatedList(projectId)
	aggregatedListCall.MaxResults(100)
	instances, err := aggregatedListCall.Do()
	if err != nil {
		return fmt.Errorf("Failed to list VM instances in %s: %v", projectId, err)
	}

	for zone, instancesInZone := range instances.Items {
		if len(instancesInZone.Instances) == 0 {
			// Zone is empty
			continue
		}

		zoneShort := zone[strings.LastIndex(zone, "/")+1:]
		fmt.Printf("%d instances found in zone %s\n", len(instancesInZone.Instances), zoneShort)

		for _, instance := range instancesInZone.Instances {
			due, err := isDueForCleanup(instance)
			if err != nil {
				return fmt.Errorf("Failed to check if VM instance is due for cleanup: %v", err)
			}

			if due {
				fmt.Printf("Cleaning up instance %s...\n", instance.Name)

				_, err := computeService.Instances.Delete(projectId, zoneShort, instance.Name).Do()
				if err != nil {
					return fmt.Errorf("Failed to delete VM instance: %v", err)
				}
			}
		}
	}

	return nil
}

// PubSubMessage is the payload of a Pub/Sub event.
type PubSubMessage struct {
	Data []byte `json:"data"`
}

func Cleanup(ctx context.Context, m PubSubMessage) error {
	return cleanupInstances(os.Getenv("GCP_PROJECT"))
}
