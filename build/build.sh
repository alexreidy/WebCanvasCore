#!/bin/bash

dotnet restore
dotnet run

cd ../src/WebCanvasCore
dotnet restore
dotnet build

if [[ ! -z $1 ]]; then
    dotnet pack
    cp bin/Debug/*.nupkg $1
    echo copied into $1
fi