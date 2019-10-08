# Module 7: Resource cleanup

Congratulations! You have now deployed the CloudMosaic sample application to your account. You may if you wish delete the application and its resources as follows. Although the tile gallery and mosaic workflow subsystems incur no charge when idle the front-end is running 24x7 in an AWS Fargate cluster and so if not being used you may want to shut them down.

1. Open the Clusters view (if not already open) onto the front-end cluster: using the AWS Explorer, expand the *Amazon Elastic Container Service* node, then expand *Clusters*. Double-click on the front-end cluster you deployed to in Module 5.
1. In the Cluster view click **Edit** in the *Services* tab.
1. Click in the *Desired tasks count* field and enter a value of **0**.
1. Click **Save**
1. The cluster will start to drain the running tasks.
1. Click **Delete** in the view. A confirmation dialog will appear.
1. Once the tasks and cluster have been deleted, expand the *Repositories* node in the AWS Explorer under *Amazon Elastic Container Service*.
1. The sample application has two repositories with name patterns following *cloud-front-**RANDOM*** and *cloud-zipex-**RANDOM***. Right click on each and select **Delete**. In the confirmation dialogs that follow check the *'Delete the repository even if it contains images* option, then click OK.
    > DANGER! Be sure to select the correct repositories belonging to the sample!
1. Expand the *AWS CloudFormation* node in the AWS Explorer. The sample consists of three stacks:
    1. ProcessRawImage
    1. *StackName**StepFunctions***
    1. *StackName*
    \
    (*StackName* is the name of the stack you used in the pre-requisites section.)
    \
    For each stack in order, right click and select **Delete** and respond to the confirmation prompt.
    > DANGER! Be sure to select the correct stacks!
1. Expand the *Amazon S3* node in the AWS Explorer and locate the bucket created by the pre-requisites stack. It will have a name with pattern *stackname*-mosaicstoragebucket-*RANDOM*. Right click and select **Delete**. In the resulting confirmation dialog check the *Delete all objects* option, then click **OK** to proceed.
    > DANGER! Be sure to select the correct bucket belonging to the sample!
1. Finally delete the user pool in Amazon Cognito. Open the AWS Management Console and navigate to the Cognito home page.
1. Click **Manage User Pools*
1. Select the user pool belonging to the sample application.
1. Click **Delete pool** and respond to the confirmation prompt.
    > DANGER! Be sure you have selected the correct pool!
1. In the AWS Management Console open the *Systems Manager* dashboard and from the list of options on the left select *Parameter Store*.
1. Delete the three parameters you added by hand in the pre-requisites section that contain details of the now-deleted user pool. They are named
    * */CloudMosaic/AWS/UserPoolId*
    * */CloudMosaic/AWS/UserPoolClientId*
    * */CloudMosaic/AWS/UserPoolClientSecret*
1. Delete the parameter related to data protection (the anti-forgery token) in ASP.NET Core views. It will have a parameter name similar to */CloudMosaic/DataProtection/key-GUID*.

***You have now completed clean up of all resources used by the sample application and concluded this guide.***
