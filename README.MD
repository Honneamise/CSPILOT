
# WORK IN PROGRESS

![image](pilot.jpg)

## CSPILOT

CSPILOT is a simple interpreter for the PILOT language ( https://en.wikipedia.org/wiki/PILOT ) written in C#

The implementation is based on the book "PILOT Users Guide" by Morrow Inc (1984)

You can find a pdf copy in the doc folder

## DIFFERENCES

Some few differences from the original PILOT of 1984 :

- Labels must be the only element of a line
- Instruction names are acceptect both LOWERCASE or UPPERCASE
- Float values are valid for numeric variables
- Compute instruction require the '#' prefix for every numeric variable 

## DEVELOPMENT NOTES: 

- TODO : make variable names NOT case sensitive
- TODO : add instructions templates for PILOT language