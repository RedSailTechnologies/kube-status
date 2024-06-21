#!/bin/bash

projects=$(find "." -type f -name "*.csproj")
for project in $projects; do
    echo "Formatting $project..."
    dotnet format $project
    echo ""
done
