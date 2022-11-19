dotnet build -c Release 
dotnet pack .\DbQueue\ -c Release -o ..\_publish
dotnet pack .\DbQueue.EntityFrameworkCore\ -c Release -o ..\_publish
dotnet pack .\DbQueue.MongoDB\ -c Release -o ..\_publish
dotnet pack .\DbQueue.Rest\ -c Release -o ..\_publish
dotnet pack .\DbQueue.Rest.Client\ -c Release -o ..\_publish
dotnet pack .\DbQueue.Grpc\ -c Release -o ..\_publish
dotnet pack .\DbQueue.Grpc.Client\ -c Release -o ..\_publish
