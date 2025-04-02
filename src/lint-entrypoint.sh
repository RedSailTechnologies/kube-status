#!/usr/bin/bash

EXIT_STATUS=0

projects=$(find "." -type f -name "*.csproj")
for project in $projects; do
    echo "Formatting $project..."
    dotnet format $project --verify-no-changes --no-restore
    LOOP_EXIT_STATUS=$?
    echo "$project Exit Status: $LOOP_EXIT_STATUS"
    EXIT_STATUS=$(($EXIT_STATUS + $LOOP_EXIT_STATUS))
done

exit $EXIT_STATUS