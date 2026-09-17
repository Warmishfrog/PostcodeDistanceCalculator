A small, lightweight WinForms app I made to literally just calculate the total distance between an array of postcodes in the uk. It uses [Api Postcodes io](https://api.postcodes.io/postcodes/) to calculate the coordinates of the postcodes, then [Project OSRM](https://project-osrm.org/) to calculate the driving distance between those coordinates

Publish using the command:
`dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true`
