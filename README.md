# PFC-Home assignment

Cloud-based restaurant menu system for ITSFT-606-1620 Programming for the Cloud.

## Included

- `MenuCloudApp/` - ASP.NET Core MVC web app for Google OAuth login, menu upload, catalog search, sorting, translation, and cache lookup.
- `cloud-functions/translation-function/` - HTTP function using Google Translation API.
- `cloud-functions/menu-processor-function/` - Pub/Sub function using Cloud Vision API and Firestore.
- `cloud-functions/menu-cleaner-function/` - HTTP function for scheduled menu cleanup and structured searchable items.

## Cloud Services Used

- Cloud Run
- Cloud Storage
- Firestore
- Pub/Sub
- Cloud Functions
- Cloud Vision API
- Cloud Translation API
- Secret Manager
- Cloud Scheduler

## Firestore Structure

```text
restaurants/{restaurantId}
restaurants/{restaurantId}/menus/{menuId}
restaurants/{restaurantId}/menus/{menuId}/images/{imageId}
```

## Run Locally

```powershell
cd MenuCloudApp
dotnet run
```

## Deploy Web App

```powershell
gcloud run deploy menu-cloud-app --source=MenuCloudApp --region=europe-west1 --allow-unauthenticated
```

## Deploy Functions

```powershell
gcloud functions deploy translateMenuItem --gen2 --runtime=nodejs22 --region=europe-west1 --source=cloud-functions/translation-function --entry-point=translateMenuItem --trigger-http --allow-unauthenticated
gcloud functions deploy processMenuUpload --gen2 --runtime=nodejs22 --region=europe-west1 --source=cloud-functions/menu-processor-function --entry-point=processMenuUpload --trigger-topic=menu-uploads-topic
gcloud functions deploy cleanPendingMenus --gen2 --runtime=nodejs22 --region=europe-west1 --source=cloud-functions/menu-cleaner-function --entry-point=cleanPendingMenus --trigger-http --allow-unauthenticated
```

## Scheduler

```powershell
gcloud scheduler jobs create http clean-pending-menus-hourly --location=europe-west1 --schedule="0 * * * *" --uri="https://europe-west1-home-assignment-pfc.cloudfunctions.net/cleanPendingMenus" --http-method=POST --headers="Content-Type=application/json" --message-body="{}"
```

OAuth secrets should be stored in Secret Manager, not committed to GitHub.
