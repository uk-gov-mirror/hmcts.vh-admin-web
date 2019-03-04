# vh-admin-web
This application provides the book a video hearing functionality for the video hearing case administrator.

# Sonar Cloud
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=vh-admin-web&metric=alert_status)](https://sonarcloud.io/dashboard?id=vh-admin-web)

# Build Status
[![Build Status](https://hmctsreform.visualstudio.com/VirtualHearings/_apis/build/status/hmcts.vh-admin-web?branchName=master)](https://hmctsreform.visualstudio.com/VirtualHearings/_build/latest?definitionId=102&branchName=master)

# Generating the clients
If the interface for either the MVC or the Bookings API is updated these can be rebuilt using the following commands:

In the `AdmniWebsite/ClientApp` folder:
```
npx nswag run api-ts.nswag
```

In the `AdminWebsite.BookingsAPI.Client` project:
```
npx nswag run booking-api-csharp.nswag 
```