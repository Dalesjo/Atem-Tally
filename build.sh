#!/bin/bash

if output=$(git status --porcelain) && [ -n "$output" ]; then
  echo "You have uncommited changes"
  exit 3
fi

# New Version number
git pull

# dotnet tool install -g dotnet-version-cli
# https://github.com/skarpdev/dotnet-version-cli
dotnet version --skip-vcs -f ./TallyClient/TallyClient.csproj patch
dotnet version --skip-vcs -f ./TallyServer/TallyServer.csproj patch

VERSION=`cat ./TallyServer/TallyServer.csproj |  grep -oPm1 "(?<=<Version>)[^<]+" | tr -d '\n'`
echo "New verison is ${VERSION}"

dotnet publish -c Release TallyClient/TallyClient.csproj
dotnet publish -c Release TallyServer/TallyServer.csproj

echo "Commiting changes"
git add ./TallyClient/TallyClient.csproj
git add ./TallyServer/TallyServer.csproj

tar -czvf images/tally-server-${VERSION}.tar.gz TallyServer/bin/Release/net6.0/win-x64/
tar -czvf images/tally-client-${VERSION}.tar.gz TallyClient/bin/Release/net6.0/linux-arm64/publish/

git commit -m "Built new Version ${VERSION}"
git push 
