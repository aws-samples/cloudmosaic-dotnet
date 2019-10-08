# Module 5: Deploy web app front end on AWS Fargate

This section deploys the web application front-end using AWS Fargate (note that we can run the front-end application either locally or after deployment to Fargate).

The web front-end demonstrates the following additional integrations with AWS:

* using the AWS extension library to retrieve runtime configuration settings from Parameter Store instead of appsettings.json
* replacing the default SQL Server identity setup with a Cognito User Pool
* using the anti-forgery data protection library from AWS that uses Parameter Store to hold the dynamically-generated keys

The project for the web front-end can be found in the *UI* solution folder.

To deploy the front-end follow the steps below.

1. Right-click on the project and select *Publish container to AWS*.
1. On the starting page of the wizard ensure the credential profile and region selections are correct and match what has been used to deploy the other subsystems above.
1. For *Docker Repository* select the repository created via the CloudFormation template in the pre-requisites section (name pattern *cloud-front-RANDOM*)
    1. For Tag enter **latest** (or leave blank)
    1. For *Deployment Target*, **Service on an ECS Cluster** should be selected.
        > Note: name pattern *CloudMosaic-ECSSecurityGroup-RANDOM*
1. Click **Next**.
1. On *Launch Configuration*:
    1. Open **VPC Subnets** and select at least 2 subnets from the VPC created for you by the CloudFormation template you deployed in module 2. If the wizard has selected any subnets from your default VPC, uncheck them.
    1. Open **Security Groups** and select the security group for the web front end (also created by the template in module 2). It will have a name similar to *CloudMosaic-ECSSecurityGroup-RANDOM*.
1. Leave the other settings at their default values and click **Next**.
1. Leave the default values unchanged on the *Service Configuration* page and click **Next**.
1. On *Application Load Balancer Configuration*:
    1. Check **Configure Application Load Balancer**.
    1. For *Load Balancer* select the one created by the pre-requisites stack (name pattern *Cloud-LoadB-RANDOM*).
    1. For *Listener Port* select **80 (HTTP)**.
    1. For *Target Group* select the one created by the pre-requisites stack (name pattern *Cloud-Defau-RANDOM*).
1. Click **Next**.
1. On *Task Definition*:
    1. For *Task Definition* select *Create new* and accept the default name, **CloudMosaicFrontend**.
    1. *Container* should be set to **CloudMosaicFrontend**.
    1. *Task Role* should be set to **Existing role: CloudMosaic-FrontendTaskRole-*RANDOM***.
    1. *Task Execution Role* should be set to **CloudMosaic-FrontendExecutionRole-*RANDOM***.
        > Note: both of these roles were created as part of the pre-requisites stack.
1. Click **Publish**.

After the wizard completes the toolkit will open a view onto the cluster. Click **Refresh** in the view's toolbar until *Running tasks* matches the number requested in the wizard (defaults to 2) and a url to the application is shown. You can now access the running application.

***You have now completed this module and can move onto the next.***
