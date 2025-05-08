#test with coverage turned on
dotnet test --settings coverlet.runsettings

#remove the old report
# rm ./report -r -force

#generate new report from coverage file
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport"

#remove the coverage xml file, no longer needed
# rm coverage.cobertura.xml

#open the coverage report
./TestResults/CoverageReport/index.html

