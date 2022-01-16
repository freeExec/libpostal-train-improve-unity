@echo off

IF "%1" EQU "" goto :exit

copy /y license_separate_addresses.tsv license_separate_addresses.tsv.%1
copy /y bitmap.dat bitmap.dat.%1

:exit
