#test with coverage turned on
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

#remove the old report
rm ./report -r -force

#generate new report from coverage file
reportgenerator -reports:coverage.cobertura.xml -targetdir:.\report -assemblyfilters:+COMPASS

#remove the coverage xml file, no longer needed
rm coverage.cobertura.xml

#open the coverage report
./report/index.html

