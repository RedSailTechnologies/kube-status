#!/usr/bin/bash

if [ -z "$1" ]
  then
    echo "Formatting all projects..."
    projects=$(find "." -type f -name "*.csproj")
  else
    echo "Formatting projects that start with $1..."
    projects=$(find "." -type f -name "$1*.csproj")
fi

for project in $projects; do
    echo "Formatting $project..."
    dotnet format $project
    echo ""
done
