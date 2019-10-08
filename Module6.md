# Module 6: Test the deployed application

In the Clusters view click the url to the deployed application to open a browser.

> Note: sometimes it may take a few seconds after the task count reaches 2 and the url appears for the application to be fully available. A 503 error page is usually the signal that we need to wait a little longer - just refresh the browser view until the application home page appears.

## Step 1: Register a user

1. Click **Register** in the application banner.
1. In the resulting form:
    1. For *UserName* use the id you used in modules 1 and 2.
    1. Enter a valid email address.
    1. Complete password etc, then click **Register**.

Cognito will now send you a code to confirm your account. To show the account is unconfirmed:

1. Open the AWS Console, navigate to the Cognito home and click **Manage User Pools**.
1. Select the pool.
1. Click **Users and groups**.
1. Show that the new user is currently unconfirmed.
1. Open your email and get the code that was mailed to you, then enter it into the field in the application's pending confirmation page.
1. Click **Confirm**.
1. Your user account is now active and logged in.
1. *(Optional)* Return to the Cognito pool page, refresh and verify that the user is now confirmed.

To login as the same user in future, click the *Login* option in the application banner and sign in with the user account you registered. Or you may register additional users.

## Step 2: Create a new tile gallery

1. Click the *Tile Galleries* link.
   > Note: If you used the same user id to register as was used in the test runs in earlier modules you should see an unnamed gallery - this is the output from the batch job test you ran in module 3.
1. Click the **Create New Gallery* link.
1. Fill in a gallery name and browse to select a zip file containing images to be added to the gallery.
    > Note: the sample application currently uses simple file upload which by default in ASP.NET Core is limited to around a 29Mb. Support for streaming uploads enabling much larger uploads will be added to the sample in future.
1. Click **Save** to start the tile ingestion process discussed in module 3.

Refresh the page after a while and you should see the gallery be declared Ready under Status (depending on the number of files in the gallery, this can take a few minutes). You can also visit the Batch dashboard in the console and view the status of the running job as well as the Lambda function being invoked as each image is extracted from the zip file and uploaded to S3.

## Step 3: Generate a mosaic

1. Click the *My Mosaics* link.
    > Note: If you used the same user id to register as was used in the test runs in earlier modules you should see an unnamed mosaic - this is the output from the test you ran in module 4.
1. Click **Create New Mosaic**.
1. Browse to select the image you want to render as a mosaic.
1. Select the tile gallery you want to use.
1. Give the mosaic an optional name.
1. Click **Save**.

You'll need to refresh the page a few times until the mosaic is listed as ready. When this happens:

1. Click the generated mosaic.
1. Zoom in to show the tiles it is made up of.

***You have now completed this module and can move onto the next.***
