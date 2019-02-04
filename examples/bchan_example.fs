FORTH-WORDLIST TASK-WORDLIST 2 SET-ORDER

100 2 NEW-BCHAN CONSTANT MY-BCHAN

: SENDER
  PAUSE-COUNT 0 BEGIN
    DUP MY-BCHAN SEND-BCHAN 1 +
    DUP $1000 MOD 0 = IF
      PAUSE-COUNT ROT OVER SWAP - $1000 SWAP / ." <" . DUP (.) ." > " SWAP
    THEN
  AGAIN ;

: RECEIVER
  PAUSE-COUNT 0 BEGIN
    MY-BCHAN RECV-BCHAN DROP 1 +
    DUP $1000 MOD 0 = IF
      PAUSE-COUNT ROT OVER SWAP - $1000 SWAP / ." [" . DUP (.) ." ] " SWAP
    THEN
  AGAIN ;

256 256 256 ' SENDER NEW-TASK CONSTANT SENDER-TASK
256 256 256 ' RECEIVER NEW-TASK CONSTANT RECEIVER-TASK

SENDER-TASK ACTIVATE-TASK RECEIVER-TASK ACTIVATE-TASK

SLEEP-TASK DEACTIVATE-TASK MAIN-TASK DEACTIVATE-TASK

\ KEY DROP SENDER-TASK DEACTIVATE-TASK RECEIVER-TASK DEACTIVATE-TASK
