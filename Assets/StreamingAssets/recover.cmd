@echo off

IF "%1" EQU "" goto :exit

copy /y license_separate_addresses.tsv.%1 license_separate_addresses.tsv
copy /y bitmap.dat.%1 bitmap.dat

:exit
