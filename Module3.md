# Module 3: Tile gallery ingestion subsystem

The steps in this module walk you through deploying and testing the tile ingestion subsystem. This subsystem enables the user to supply a zip file containing images to be used as tiles when creating a mosaic. An AWS Batch job expands the zip file and uploads each image in the file to an Amazon S3 bucket (created as part of the application resource stack you deployed in module 2). An AWS Lambda function is triggered by each upload to validate and process the image files to make them suitable to use as tiles in a mosaic.

The architecture for this subsystem is shown below.

![Tile gallery architecture](media/3-Architecture.png)

## Why we chose AWS Batch

* Tile ingestion is not a time-critical task and using AWS Batch gives us flexibility to choose appropriate compute resources, for example:
  * We could use spot instances to do the work when compute becomes available at a cost we define.
  * We could enable paying customers to get access to more powerful (and on-demand) compute resources.
  * The batch process starts when work is available in the queue, does what's needed, and then closes down again. There is no charge when the code is not actually running.

Note that the AWS Lambda function also does not run until objects are available to process - so there is no idle time cost.

The projects to be used in this module can be found under the *GalleryGenerator* subfolder.

## Step 1: Deploy the *ZipExpanderConsole* project to Elastic Container Registry

The *ZipExpanderConsole* project in the solution is a simple .NET Core console application that takes a URL to a zip file containing images to be used in a tile gallery. The job unpacks the zip file and uploads each image it contains to an Amazon S3 bucket.

The batch job uploads the individual images to the bucket, placing each image under a 'Galleries/Raw' S3 key prefix. The bucket to be used, created by the AWS CloudFormation template you deployed in module 2, has a name following the pattern *stackname*-mosaicstoragebucket-*RANDOM* in your account (for example, *cloudmosaic-mosaicstoragebucket-mpxc9qo7wja9*).

> Note: If you changed the name of the stack from the suggested default of *CloudMosaic*, be sure to select the correct bucket in the instructions below.

You can deploy the batch job project using either a wizard in Visual Studio, or the dotnet CLI.

### (Option 1) Deploying the ZipExpander project using Visual Studio

1. In Solution Explorer, right-click on the *ZipExpanderConsole* project and select *Publish container to AWS*.
1. Select the credential profile to be used (set up in module 1)
1. Select the region you chose when deploying the application resources CloudFormation template in module 2.
1. Select the repository to deploy to - it has a name following the pattern *cloud-zipex-*RANDOM**.
1. Leave tag blank, or enter **latest**
1. Deployment target should be set to *Push only the Docker image...*

    ![Publish ZipExpander project](media/3-PublishZipExpander.png)

1. Click Publish to push the image and exit the wizard. You can now proceed with step 2 in this module.

### (Option 2) Deploying the ZipExpander project using the dotnet CLI

1. Open a command shell and cd into the *./Application/GalleryGenerator/ZipExpanderConsole* folder.
1. You should have already installed the Amazon.ECS.Tools global tool package as part of module 1 but if not, run the following command to install:

    ```bash
    dotnet tool install -g Amazon.ECS.Tools
    ```

1. Edit the *aws-ecs-tools-defaults.json* file in the project to set the correct tag value identifying the repository and other resources for the deployment. In the example below, replace the value of the key *tag* with the correct value, which you can obtain from the resources created in the CloudFormation stack you deployed in module 2.

    ```json
    {
        "region" : "us-west-2",
        "profile" : "default",
        "configuration" : "Release",
        "tag" : "cloud-zipex-EDIT-ME:latest",
        "task-definition-name" : "",
        "task-cpu"             : "512",
        "task-memory"          : "1024",
        "task-definition-task-role" : "",
        "task-execution-role" : "ecsTaskExecutionRole",
        "vstoolkit-deployment-mode" : "PushOnly",
        "docker-build-working-dir"  : ""
    }
    ```

1. Save the file and exit to the command shell.
1. Run the following command to perform the deployment:

    ```bash
    dotnet ecs push-image
    ```

## Step 2: Deploy the ProcessRawImage Lambda Function project

The *ProcessRawImage* project is an AWS Lambda function, written in C#, to accompany the batch job. It is invoked when an object is created or updated in an Amazon S3 bucket - as a result of the zip expander running - under a specific key path.

The Lambda function uses Amazon Rekognition to check for moderation labels and if there are any, it abandons further processing of the image. If the image passes moderation checks the function proceeds to determine an average color before resizing and storing as a tile belonging to the user's gallery.

To deploy the Lambda function, you can use either a wizard in Visual Studio, or the dotnet CLI.

### (Option 1) Deploying the ProcessRawImage project using Visual Studio

1. In Solution Explorer, right-click on the ProcessRawImage project, and select *Publish to AWS Lambda*
    1. Choose an S3 bucket to contain the uploaded bundle (the bucket must be in same region as the deployment is targeting). You can create a new bucket if you wish or use an existing one.

        > Note: the bucket you select **is not** the same bucket as will be used by the batch job to upload images.

    1. Leave all other settings on the first page at their defaults

    ![Publish ProcessRawImage page 1](media/3-PublishProcessRawImage1.png)

    1. Click **Next**
    1. The *TableGalleryItems* parameter must be set to the name of the DynamoDB table holding gallery items. This table was defined in the application resource template you deployed to CloudFormation in module 2. If the suggested stack name of *CloudMosaic* was used, this table will be named *CloudMosaic-GalleryItem*. If you changed the stack name from the suggested default, be sure to set the correct name for the table before proceeding.

    ![Publish ProcessRawImage page 2](media/3-PublishProcessRawImage2.png)

    1. Click **Publish**

With the Lambda function deployed, your next step is to wire up an event notification on the bucket so that object create or update events cause your function to be invoked. You can find the instructions for this in the section titled *Set up the event notification trigger* below.

### (Option 2) Deploying the ProcessRawImage project using the dotnet CLI

1. Open a command shell and cd into the *./Application/GalleryGenerator/ProcessRawImage* folder.
1. You should have already installed the Amazon.Lambda.Tools global tool package as part of module 1 but if not, run the following command to install:

    ```bash
    dotnet tool install -g Amazon.Lambda.Tools
    ```

1. Edit the *aws-lambda-tools-defaults.json* file in the project to set the correct parameters (profile, region) and Amazon S3 bucket that will be used during the deployment, for example:

    ```json
    {
        "profile"     : "default",
        "region"      : "us-west-2",
        "configuration" : "Release",
        "framework"     : "netcoreapp2.1",
        "s3-prefix"     : "ProcessRawImage/",
        "template"      : "serverless.template",
        "template-parameters" : "\"TableGalleryItems\"=\"CloudMosaic-GalleryItem\"",
        "s3-bucket"           : "YOUR-BUCKET-NAME-HERE",
        "stack-name"          : "ProcessRawImage"
    }
    ```

    > Note: the S3 bucket must be in the same region as the Lambda function you are creating. The S3 bucket **is not** the same bucket as the one created by the application resource template, for use by the batch job, in module 2.
    \
    \
    > Note: The *TableGalleryItems* parameter must be set to the name of the DynamoDB table holding gallery items. This table was defined in the application resource template you deployed to CloudFormation in module 2. If the suggested stack name of *CloudMosaic* was used, this table will be named *CloudMosaic-GalleryItem*. If you changed the stack name from the suggested default, be sure to set the correct name for the table before proceeding.

1. Save the file and exit to the command shell.
1. Run the following command to perform the deployment:

    ```bash
    dotnet lambda deploy-serverless
    ```

With the Lambda function deployed, your next step is to wire up an event notification on the bucket so that object create or update events cause your function to be invoked.

## Set up the event notification trigger

When the deployment (from Visual Studio or the command line) has completed and status reads **CREATE-COMPLETE**, the next step is to wire up an S3 event notification to the newly deployed Lambda function. This will cause the Lambda function to be invoked when objects are created or updated under a specified key path in the bucket.

### (Option 1) Set up the event notification using Visual Studio

1. In Visual Studio, refresh the AWS Explorer view and the expand the *AWS Lambda* tree. Select the *ProcessRawImage* function and double-click it to open the function view.
1. In the function view click the *Event Sources* tab
1. Click **Add**
1. Set *Source Type* to **Amazon S3**
1. For *S3 Bucket* choose the bucket created by the application resources template deployed in module 2. The bucket will have a name with the pattern *cloudmosaic-mosaicstoragebucket-RANDOM*, if you used the suggested stack name. If you changed the stack name, locate and select the correct bucket.
1. Enter **Galleries/Raw** in the *Prefix* field. Only objects that are created or updated under this specific key path will trigger the event.

    ![Configure event source](media/3-EventSource.png)

1. Click OK

You are now ready to test your batch job, and can skip to step 3 in this module.

### (Option 2) Set up the event notification using the AWS Management Console

1. In the the [AWS Management Console](https://console.aws.amazon.com/), navigate to the console home and enter *lambda* into the search field. Select the **Lambda** entry that is displayed, to navigate to the Lambda console home.
1. Click the *ProcessRawImage* function to open its detail pages. The deployed function will have a name following the pattern *ProcessRawImage-ProcessRawImageFunction-RANDOM*.
1. Click **Add trigger**.
1. In *Select a trigger*, choose **S3**.
1. In *Bucket*, choose the bucket created by the application resources template deployed in module 2. The bucket will have a name with the pattern *cloudmosaic-mosaicstoragebucket-RANDOM*, if you used the suggested stack name. If you changed the stack name, locate and select the correct bucket.
1. For *Event type* ensure **All object create events** is selected.
1. For *Prefix* enter the key path **Galleries/Raw**.

    ![Configure event source](media/3-EventSource-LambdaConsole.png)

1. Click **Add** to complete the notification setup.

## Step 3: Test the Batch job

In this step you will simulate what the web application will do when a logged-in user uploads a zip file containing images to create a tile gallery, to verify your deployments of the batch job and associated Lambda function.

To support these steps the repository contains a small collection of solid color swatches, *./TileGalleries/ColorSwatches.zip*, that you can use to create a tile gallery. If you have your own zip file of images you can also use that instead.

1. In the [AWS Management Console](https://console.aws.amazon.com/), navigate to the console home page. In the *Services* field enter the text **Batch**.
    * Select *Batch* from the results to navigate to the AWS Batch console home page.
1. Click **Create job**.
1. Give the job a name.
1. For *Job definition* select the definition created by the CloudFormation template deployed in module 2. This used a naming pattern of *ZipExpanderJobDefinition-RANDOM*.
1. For *Job queue* select the queue created by the CloudFormation template deployed in module 2. This used a naming pattern of *ZipExpanderJobQueue-RANDOM*.

    ![Batch job settings](media/3-BatchJobSettings1.png)

The batch job uses fives environment variables defining:

* the url to the zip file it should process
* the bucket to which it should upload images unpacked from the zip file
* the DynamoDB table that will be updated with details of the upload images
* a logical name for the tile gallery
* the id of a user who owns the gallery (although we don't have real users yet)

> Note: these variables will be set from the web UI in the fully deployed application. We need to set them here by hand as we are queuing the job manually for test purposes.

To enter this data, scroll down in the batch job settings page and enter the environment variables one at a time, as noted below. A screenshot of a completed set of variables is shown after the list for reference.

> Note: the values shown below assume you used the default stack name of *CloudMosaic* when you deployed the application resources stack in module 2. If you changed the stack name, be sure to set the correct values for the variables.

* Variable 1: key = *ZIP_EXPANDER_BUCKET*, value from application resources stack (e.g. *cloudmosaic-mosaicstoragebucket-RANDOM*)
* Variable 2: key = *ZIP_EXPANDER_GALLERY_ID*, value = *gallery-name-of-your-choice*
* Variable 3: key = *ZIP_EXPANDER_USER_ID*, value = *your-intended-user-id*
    > Note: choose the id you'll use eventually in the front-end app to have the gallery be available to that user
* Variable 4: key = *ZIP_EXPANDER_DDB_TABLE*, value = *CloudMosaic-Gallery*
    > Note: the DynamoDB table name is case sensitive, so be sure to match the casing of the stack name used in the pre-requisites section.
* Variable 5: key = *ZIP_EXPANDER_IMPORT_URL*, value = **url to zip file somewhere**

To generate a url you can use the Visual Studio toolkit to upload a file to an S3 bucket, and generate a pre-signed url to it, as follows:

* Double click the bucket in the AWS Explorer to open it.\
* Drag a zip file containing images to be used as tiles and drop it on the bucket view. Click **OK** in the dialog box that is displayed to start the upload.\
* Once the upload completes right click on the new object and select **Create Pre-Signed URL**.\
* In the dialog that is displayed, leave the settings at their defaults and click **Generate**.\
* Copy the url that is displayed then close the dialog.\
* Paste the url into the environment variable value for the batch job.

When you have entered all 5 variables, your screen should look similar to the below.

![Batch Job environment settings](media/3-BatchJobSettings2.png)

Click **Submit job** to start the batch process. You can monitor progress from the batch job details page.

> Note: there is no 'done' notification from either the Batch job or the Lambda function. Wait until the job is listed under *succeeded* on the job console before proceeding to satisfy yourself that the ingestion process is working correctly.

***You have now completed this module and can move onto [module 4](./Module4.md).***
