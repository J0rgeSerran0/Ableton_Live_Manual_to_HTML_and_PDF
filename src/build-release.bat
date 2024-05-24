echo Executing AbletonLiveManualToPDF...
@echo Started: %date% %time%

dotnet build -p:Version=1.1.0 -c:Release

@echo Completed: %date% %time%
