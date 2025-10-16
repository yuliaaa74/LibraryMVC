
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app


COPY LibraryMVC/*.csproj ./LibraryMVC/
RUN dotnet restore ./LibraryMVC/


COPY . .


RUN dotnet tool install --global dotnet-ef

ENV PATH="$PATH:/root/.dotnet/tools"



WORKDIR /app/LibraryMVC


RUN dotnet publish -c Release -o /app/out


WORKDIR /app/out


ENTRYPOINT ["dotnet", "LibraryMVC.dll"]
