@echo off

IF "%1" EQU "" goto :exit

copy /y license_separate_addresses.tsv.%1 license_separate_addresses.tsv
copy /y btimap.dat.%1 btimap.dat

:exit
