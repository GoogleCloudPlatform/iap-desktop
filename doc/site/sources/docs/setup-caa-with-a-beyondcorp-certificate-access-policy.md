# Use BeyondCorp certificate-based access

IAP Desktop supports 
[BeyondCorp certificate-based access :octicons-link-external-16:](https://cloud.google.com/beyondcorp-enterprise/docs/securing-resources-with-certificate-based-access)
and can use mutual TLS to connect to Google Cloud resources.

!!!note

    This feature requires a BeyondCorp subscription. For a comparison between the
    features available to Google Cloud customers and what is available with BeyondCorp Enterprise,
    see [BeyondCorp Enterprise pricing :octicons-link-external-16:](https://cloud.google.com/beyondcorp-enterprise/pricing).

## Enable certificate-based access

To enable certificate-based access in IAP Desktop, do the following:

1.  In the application, select **Tools > Options**.
1.  Select **Secure connections to Google Cloud by using certificate-based access**.    
1.  Click **OK**.
1.  Close IAP Desktop and launch it again.

If you're using Active Directory, you can also [configure a group policy](group-policies.md)
to automatically enable certificate-based access for your users.

## Certificate selection

IAP Desktop automatically determines the right certificate to use based on 
the [Chrome `AutoSelectCertificateForUrls`](https://cloud.google.com/beyondcorp-enterprise/docs/enable-cba-enterprise-certificates#configure_users_browser_to_use_your_enterprise_certificate)
policy. You can manage this policy using the [Chrome group policy templates :octicons-link-external-16:](https://support.google.com/chrome/a/answer/187202).

If you're using the [Endpoint Verification client helper :octicons-link-external-16:](https://cloud.google.com/endpoint-verification/docs/deploying-with-third-party-tools)
instead of enterprise certificates, IAP Desktop uses the certificate issued by the client helper.

!!!note
    IAP Desktop does not support Chrome Browser Cloud Management. You must configure the
    `AutoSelectCertificateForUrls` policy using a group policy.
