# Module 2: Setup application roles and settings

With the tools and your development credentials setup completed, in this and subsequent modules you will proceed to deploy the application components to AWS.

## Setup pre-requisite application roles and other configuration resources

The sample application is hosted in a [virtual private cloud (VPC)](https://aws.amazon.com/vpc/) and requires several role and other configuration resources to be created. We have defined these assets in an AWS CloudFormation template that you can deploy as follows. You can view the template and the resources it defines by inspecting the file *Application/CloudFormationTemplates/CloudMosaic-resources.template* in the solution.

### (Option 1) Deploying the resources stack using Visual Studio

1. To deploy the stack from within Visual Studio, in the solution, select the *Application/CloudFormationTemplates/CloudMosaic-resources.template* file and right-click to view its context menu. Select *Deploy to AWS CloudFormation* from the menu. This will launch the deployment wizard shown below.
1. In the wizard, select the credentials to deploy with, and the region in which the resources will be created.

    > Note: You set up these credentials in a credential profile in module 1. Select the name of the profile in the *Account profile to use* field.

1. Enter a name for the stack.

    > Note: We suggest using the name **CloudMosaic**. The stack name is also used to name many of the resources it creates that will be refer to in later modules. If you choose a different name, be sure to make a note and to adjust the instructions in later modules as required.

    ![Deploy to AWS CloudFormation](media/2-DeploymentWizard.png)

1. Click **Next** to review the settings, then click **Finish** to close the wizard and start the deployment.

The resources defined in the stack will take a few minutes to complete creation. While you wait, you can jump to *Setup the Amazon Cognito User Pool* below to continue.

### (Option 2) Deploying the resources stack using the AWS Management Console

1. Log into the the [AWS Management Console](https://console.aws.amazon.com/) using the development credentials you set up in module 1 and from the console home page, enter *CloudFormation* into the search field. Select the **CloudFormation** entry that is displayed, to navigate to the CloudFormation console home.
1. Be sure to select the correct region you want to deploy to, using the region indicator at the top right of the page.
1. Click **Create stack** and select the *with new resources* option.
1. Under *Specify template*, select **Upload a template file**. Click **Choose file** and navigate to the *Application/CloudFormationTemplates/CloudMosaic-resources.template* file in your copy of the repository. Click **Next**.
1. Enter a name for the stack, then click **Next**.

    > Note: We suggest using the name **CloudMosaic**. The stack name is also used to name many of the resources it creates that will be refer to in later modules. If you choose a different name, be sure to make a note and to adjust the instructions in later modules as required.

1. On the final page, be sure to check the **I acknowledge that AWS CloudFormation might create IAM resources** option, under *Capabilities*, and then click **Create stack** to finish.

The resources defined in the stack will take a few minutes to complete creation. While you wait, you continue to setup other resources in the *Setup the Amazon Cognito User Pool* section below.

## Setup the Amazon Cognito User Pool

While the stack is being created, setup a new Amazon Cognito User Pool. This user pool will be used to users to register and login to the sample application when it is deployed. Create the user pool by following the instructions below.

1. Navigate to the console home page in the [AWS Management Console](https://console.aws.amazon.com/)
    * Select *Services* and enter the text **Cognito** into the search bar.
    * Click the resulting entry that is returned.
1. Click **Manage User Pools**
1. Click **Create a user pool**
1. Give the pool a name, then click **Review defaults**
1. Click **Add app client...**
1. Enter a name for the client (for example *CloudMosaicWebUI*), leave the rest of the settings at their default values and then click **Create app client**.
1. Click **Return to pool details**.
1. Click **Create pool**.
1. After the pool has been created make a note of the following values:
    * From the *General Settings* page:
        * **User Pool id**

        ![User pool id](media/2-UserPoolSettings1.png)

    * From the App Clients page (click *App clients* in left-hand navigation pane to view this page):
        * **App client id**
        * **App client secret** (click *Show Details* to view)

        ![User pool id](media/2-UserPoolSettings2.png)

## Record the user pool details in Systems Manager's Parameter Store

The web front-end for the application will retrieve the details of the user pool for user management at run time, by making calls to [Systems Manager's Parameter Store](https://aws.amazon.com/systems-manager/features/#Parameter_Store). Parameter Store is a simple key-value store that can be used to store both plain text and secure string values. In this section you will create three parameters to hold the data identifying the user pool for the application to use.

1. Navigate to the Systems Manager dashboard in the [AWS Management Console](https://console.aws.amazon.com/)
    * Select *Services* and enter the text **Systems** into the search field.
    * Select *Systems Manager* from the results.
1. From the Systems Manager dashboard, scroll through the options on the left navigation panel and select *Parameter Store*.
1. Create three parameters, one by one, for the user pool values you made a note of above, as follows:

    * From the Parameter Store home, click **Create parameter**
    * For the parameter name, enter **/CloudMosaic/AWS/UserPoolId**
    * Set the parameter type to be *String*
    * For the parameter value, enter the value you recorded for *User pool id* above, then click **Create parameter** to finish. For example:

        ![Parameter store](media/2-ParameterStore.png)


    * Repeat the process to create a second parameter. This time set the parameter name to be **/CloudMosaic/AWS/UserPoolClientId**, the parameter type to be *String* and the value should be set to the *App client id* value you recorded above.
    * Repeat the process a final time to create the third parameter. For parameter name, enter **/CloudMosaic/AWS/UserPoolClientSecret**. For the parameter type, select *SecureString*. Leave the *KMS key source* and *KMS Key ID* fields at their defaults. In the Value field, enter the value of the *App client secret* you recorded earlier.

***You have now completed this module and can move onto [module 3](./Module3.md).***
