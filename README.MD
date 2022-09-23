
# WORK IN PROGRESS

## CSPILOT

CSPILOT is a simple interpreter for the PILOT language ( https://en.wikipedia.org/wiki/PILOT ) written in C#

The implementation is based on the book "PILOT Users Guide" by Morrow Inc (1984)

You can find a pdf copy in the doc folder


## DEVELOPMENT NOTES: 
---
URGENTE : FIXARE PARSEINTRUCTION ON BODY == NULL

---
per la WAIT provare :

int a = Console.In.Peek();

while ( a==Console.In.peek && UN TIMER CHE SCADE DOPO 6 SEC )

---
per la dimensione della console vedere dettagli di :

Console.WindowWidth
ti da il numero di colonne, se si lascia fare il resize, bisogna controllare ciclicamente il "size"

Console.SetWindowSize(w, h)

si puo' bloccare la dimensione della console ?