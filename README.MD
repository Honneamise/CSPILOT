
# CSPILOT

![image](pilot.jpg)

## CSPILOT

CSPILOT is a simple interpreter for the PILOT language ( https://en.wikipedia.org/wiki/PILOT )

The implementation is based on the 1984 book "PILOT Users Guide" by Morrow Inc ( You can find a pdf copy in the doc folder )

## NOTES

Notes on the current implementation :

- The current statements format is :

```
INSTRUCTION[CONDITION]:[PARAMETERS]
```

- The optional heading label of instructions has been removed

- Any arbitrary number of leading or trailing blanks is allowed between elements, except between INS[COND] if COND is present

- Instructions, Conditions, Labels and Variables must be UPPERCASE

- Floats are valid as value for Numeric Variables

- The CASE instruction have the following format :

```
CASE: #VARIABLE *LABEL1, *LABEL2, *LABEL3, etc...
```

- The COMPUTE instruction have mandatory __'#'__ prefix for every numeric variable involved

- The functions for the cursor are available only on windows platform

- CP/M instructions are not available

## DEVELOPMENT NOTES: 

- TODO : ideas for alternative Console.readlin ???

- TODO : complete the PILOT instructions

- TODO : complete the MANUAL entries for PILOT instructions
