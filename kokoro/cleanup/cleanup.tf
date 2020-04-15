#------------------------------------------------------------------------------
# Input variables
#------------------------------------------------------------------------------

variable "project_id" {
    description = "Project to deploy in"
    type = string
}

variable "region" {
    description = "Region to deploy in"
    type = string
    default = "us-central1"
}

#------------------------------------------------------------------------------
# Preparing the project
#------------------------------------------------------------------------------

provider "archive" {
}

provider "google" {
    project         = var.project_id
    region          = var.region
}

#------------------------------------------------------------------------------
# IAM configuration
#------------------------------------------------------------------------------

resource "google_service_account" "cleanup" {
  account_id   = "cleanup"
  display_name = "Cleanup"
}

resource "google_project_iam_member" "cleanup_service_account_iam" {
  role    = "roles/compute.instanceAdmin.v1"
  member  = "serviceAccount:${google_service_account.cleanup.email}"
}

#------------------------------------------------------------------------------
# Trigger
#------------------------------------------------------------------------------

resource "google_pubsub_topic" "cleanup" {
  project     = var.project_id
  name = "cleanup"
}

resource "google_cloud_scheduler_job" "cleanup-schedule" {
  project     = var.project_id
  name        = "cleanup"
  description = "Cleanup temporary VM instances"
  schedule    = "*/2 * * * *"

  pubsub_target {
    topic_name = google_pubsub_topic.cleanup.id
    data      = base64encode("{data: null}")
  }
}

#------------------------------------------------------------------------------
# Cloud Function
#------------------------------------------------------------------------------

resource "google_storage_bucket" "code-bucket" {
  name = "${var.project_id}-cleanup"
}

data "archive_file" "code_zip" {
  type        = "zip"
  source_dir  = "src/"
  output_path = "obj/function.zip"
}

resource "google_storage_bucket_object" "code" {
  # Use a file name that changes when the code in the archive changes so that 
  # the Cloud Function is redeployed.
  name = "${data.archive_file.code_zip.output_base64sha256}.zip"
  bucket = google_storage_bucket.code-bucket.name
  source = "obj/function.zip"
}

resource "google_cloudfunctions_function" "cleanup-instances" {
  name        = "cleanup-instances"
  description = "Cleanup unused instances"
  runtime     = "go111"
  entry_point = "Cleanup"

  service_account_email = google_service_account.cleanup.email
  available_memory_mb   = 128
  source_archive_bucket = google_storage_bucket.code-bucket.name
  source_archive_object = google_storage_bucket_object.code.name
  
  event_trigger {
    event_type          = "providers/cloud.pubsub/eventTypes/topic.publish"
    resource            = google_pubsub_topic.cleanup.name
  }
}