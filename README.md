# stieviewer.NET
Stieviewer.NET is a C# rework of stieviewer, allowing .NET applications to interface with the stievie api.
The center Stieviewer.NET is the dynamic link library (.dll). This library contains all data from the API, enriched by linking data together.

Usage: Download and compile the library (be.stieviewer.stievie). When this is completed, you can include the library in your project. Accessing the Stievie API is done by performing the following steps in the correct order:
- Login
- Get channels
- Get EPG

Now you can use the EPG to open the correct streams and watch live TV.
