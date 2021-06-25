Remove-Item -LiteralPath "./deploy" -Force -Recurse
dotnet pack -c Release -o ./deploy
Get-ChildItem ./deploy/ | foreach { dotnet nuget push $_.FullName --skip-duplicate -k $(cat ./apiKey.txt) --source https://api.nuget.org/v3/index.json }  
