# Files and folders extraction sample

![Platforms](https://img.shields.io/badge/platform-Windows|MacOS-lightgray.svg)
![.NET](https://img.shields.io/badge/.NET%20Core-3.1-blue.svg)
[![License](http://img.shields.io/:license-MIT-blue.svg)](http://opensource.org/licenses/MIT)

[![oAuth2](https://img.shields.io/badge/oAuth2-v1-green.svg)](http://developer.autodesk.com/)
[![Data-Management](https://img.shields.io/badge/Data%20Management-v2-green.svg)](http://developer.autodesk.com/)
[![BIM360](https://img.shields.io/badge/BIM360-v1-green.svg)](http://developer.autodesk.com/)
[![ACC](https://img.shields.io/badge/ACC-v1-green.svg)](http://developer.autodesk.com/)

[![MongoDB](https://img.shields.io/badge/MongoDB%20Atlas-4-darkgreen.svg)](https://aws.amazon.com/elasticsearch-service/)

![Advanced](https://img.shields.io/badge/Level-Advanced-red.svg)

# Description

This sample demonstrate how to retrieve data of all the folders and files on a specific project to render on a table and export it as csv. This sample recursively iterate through all folders of the selected project. It then, stores the data on a MongoDB collection temporarily, until the client retrieves that to render on its page. By the end of the extraction, every file and folder under the selected project are rendered on the user table and can be exported as csv.

## Thumbnail

![](thumbnail.gif)

## Live version

Try it at https://filesnfoldersextraction.herokuapp.com

# Setup

## Prerequisites

1. **Forge Account**: Learn how to create a Forge Account, activate subscription and create an app at [this tutorial](http://learnforge.autodesk.io/#/account/).
2. **Visual Studio**: Either Community (Windows) or Code (Windows, MacOS).
3. **.NET Core** basic knowledge with C#
4. **MongoDB**: noSQL database, [learn more](https://www.mongodb.com/). Or use a online version via [mLab](https://mlab.com/) (this is used on this sample)
5. **HangFire**: Library for dealing with queueing. [learn more] (https://www.hangfire.io).

## Running locally

Clone this project or download it. It's recommended to install [GitHub desktop](https://desktop.github.com/). To clone it via command line, use the following (**Terminal** on MacOSX/Linux, **Git Shell** on Windows):

    git clone https://github.com/JoaoMartins-Forge/filesnfoldersextraction

**MongoDB**

[MongoDB](https://www.mongodb.com) is a no-SQL database based on "documents", which stores JSON-like data. For testing purpouses, you can either use local or live. For cloud environment, try [MongoDB Atlas](https://www.mongodb.com/cloud/atlas) (offers a free tier). With MongoDB Atlas you can set up an account for free and create clustered instances, intructions:

1. Create a account on MongoDB Atlas.
2. Under "Collections", create a new database (e.g. named `filesnfoldersextraction`) with a collection (e.g. named `items`).
3. Under "Command Line Tools", whitelist the IP address to access the database, [see this tutorial](https://docs.atlas.mongodb.com/security-whitelist/). If the sample is running on Heroku, you'll need to open to all (IP `0.0.0.0/0`). Create a new user to access the database.

At this point the connection string should be in the form of `mongodb+srv://<username>:<password>@clusterX-a1b2c4.mongodb.net/inventor2revit?retryWrites=true`. [Learn more here](https://docs.mongodb.com/manual/reference/connection-string/)

There are several tools to view your database, [Robo 3T](https://robomongo.org/) (formerly Robomongo) is a free lightweight GUI that can be used. When it opens, follow instructions [here](https://www.datduh.com/blog/2017/7/26/how-to-connect-to-mongodb-atlas-using-robo-3t-robomongo) to connect to MongoDB Atlas.

**Visual Studio** (Windows):

Right-click on the project, then go to **Debug**. Adjust the settings as shown below. For environment variable, define the following:

- ASPNETCORE_ENVIRONMENT: `Development`
- FORGE_CLIENT_ID: `your id here`
- FORGE_CLIENT_SECRET: `your secret here`
- FORGE_CALLBACK_URL: `http://localhost:3000/api/forge/callback/oauth`
- ITEMS_COLLECTION: `your collection for temporary storage of files and folders`
- HANGFIREDATABASE: `Hangfire database name`
- MONGO_CONNECTOR: `your mongodb connection string`
- ITEMS_DB: `your items database name`

![](forgeSample/wwwroot/img/readme/visual_studio_settings.png)

**Visual Sutdio Code** (Windows, MacOS):

Open the folder, at the bottom-right, select **Yes** and **Restore**. This restores the packages (e.g. Autodesk.Forge) and creates the launch.json file. See _Tips & Tricks_ for .NET Core on MacOS.

![](forgeSample/wwwroot/img/readme/visual_code_restore.png)

At the `.vscode\launch.json`, find the env vars and add your Forge Client ID, Secret and callback URL. Also define the `ASPNETCORE_URLS` variable. The end result should be as shown below:

```json
"env": {
  "ASPNETCORE_ENVIRONMENT": "Development",
  "ASPNETCORE_URLS" : "http://localhost:3000",
  "ITEMS_COLLECTION": "your collection for temporary storage of files and folders",
  "FORGE_CALLBACK_URL": "http://localhost:3000/api/forge/callback/oauth",
  "FORGE_CLIENT_SECRET": "your client secret here",
  "HANGFIREDATABASE": "Hangfire database name",
  "MONGO_CONNECTOR": "your mongodb connection string",
  "FORGE_CLIENT_ID": "your client Id here",
  "ITEMS_DB": "your items database name"
},
```

Open `http://localhost:3000` to start the app. Select **Index my BIM 360 Account** before using (this process may take a while). Check the `http://localhost:3000/dashboard` to see the jobs running (Hangfire dashboard).

## Deployment

To deploy this application to Heroku, the **Callback URL** for Forge must use your `.herokuapp.com` address. After clicking on the button below, at the Heroku Create New App page, set your Client ID, Secret and Callback URL for Forge.

[![Deploy](https://www.herokucdn.com/deploy/button.svg)](https://heroku.com/deploy)

# Further Reading

Documentation:

- [BIM 360 API](https://developer.autodesk.com/en/docs/bim360/v1/overview/) and [App Provisioning](https://forge.autodesk.com/blog/bim-360-docs-provisioning-forge-apps)
- [Data Management API](https://developer.autodesk.com/en/docs/data/v2/overview/)

Other APIs:

- [Hangfire](https://www.hangfire.io/) queueing library for .NET
- [MongoDB for C#](https://docs.mongodb.com/ecosystem/drivers/csharp/) driver
- [Mongo Altas](https://www.mongodb.com/cloud/atlas) Database-as-a-Service for MongoDB

### Tips & Tricks

This sample uses .NET Core and works fine on both Windows and MacOS, see [this tutorial for MacOS](https://github.com/augustogoncalves/dotnetcoreheroku).

### Troubleshooting

1. **Cannot see my BIM 360 projects**: Make sure to provision the Forge App Client ID within the BIM 360 Account, [learn more here](https://forge.autodesk.com/blog/bim-360-docs-provisioning-forge-apps). This requires the Account Admin permission.

2. **error setting certificate verify locations** error: may happen on Windows, use the following: `git config --global http.sslverify "false"`

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.

## Written by

João Martins [@JooPaulodeOrne2](http://twitter.com/JooPaulodeOrne2), [Forge Partner Development](http://forge.autodesk.com)
