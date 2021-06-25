Remove-Item -LiteralPath "./deploy" -Force -Recurse
dotnet pack -c Release -o ./deploy
dotnet nuget push "./deploy/*.nupkg" --skip-duplicate -k $(cat ./apiKey.txt)
