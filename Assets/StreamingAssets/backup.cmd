@echo off

IF "%1" EQU "" goto :exit

copy /y license_separate_addresses.tsv license_separate_addresses.tsv.%1
copy /y btimap.dat btimap.dat.%1

:exit