 # AzureFunction-CarImageAPI
A simple Azure Function App that acts as an API that can be called to get images of cars from Blob Storage based on vehicle numberplate &amp; image angle parameters (and optionally width and height parameters for image resizing).

## Getting started
All you need to get started with this Function app is the following:

1. Create a Storage Account in Azure and add a Blob container for each car - with the naming scheme of the reg numbers as the container names (for example, "AB24ABC").
2. In each of these containers, add some images of cvarious angles of the car. These images must be in .jpg format (but you can change this in the Function code if needed), and should follow similar naming schemes to "front", "rear", "left-side", "right-side" etc.
3. Once you've created these containers and added the images, grab the Storage Account Name and Key from the Access Keys tab in the Storage account blade
4. Open up the Solution in Visual Studio, and open the GetCarImage.cs file
5. Paste in your Storage Account Name and Key into the code where the comments indicate
6. Save the file and then right-click the Solution in the Solution Explorer, and click Publish
7. Fill in the new profile for the Function App you want to create with the app name, desired resource group etc., then click Publish (alternatively you can do this in the Azure portal and import the code manually)
8. Get the URL for your new Function from the Functions app blade in the Azure portal, and send a GET or POST request from Postman (or your favourite API testing tool) to the URL, specifying a valid numberplate and angle in your request like so: myfunctionapp.azurewebsites.net/AB24ABC/front

Enjoy!
