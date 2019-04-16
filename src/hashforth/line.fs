\ Copyright (c) 2019, Travis Bemann
\ All rights reserved.
\ 
\ Redistribution and use in source and binary forms, with or without
\ modification, are permitted provided that the following conditions are met:
\ 
\ 1. Redistributions of source code must retain the above copyright notice,
\    this list of conditions and the following disclaimer.
\ 
\ 2. Redistributions in binary form must reproduce the above copyright notice,
\    this list of conditions and the following disclaimer in the documentation
\    and/or other materials provided with the distribution.
\ 
\ 3. Neither the name of the copyright holder nor the names of its
\    contributors may be used to endorse or promote products derived from
\    this software without specific prior written permission.
\ 
\ THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
\ AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
\ IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
\ ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
\ LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
\ CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
\ SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
\ INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
\ CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
\ ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
\ POSSIBILITY OF SUCH DAMAGE.

GET-ORDER GET-CURRENT BASE @

DECIMAL
FORTH-WORDLIST 1 SET-ORDER
FORTH-WORDLIST SET-CURRENT

WORDLIST CONSTANT LINE-WORDLIST

\ Character constants
$01 CONSTANT CTRL-A
$02 CONSTANT CTRL-B
$04 CONSTANT CTRL-D
$05 CONSTANT CTRL-E
$06 CONSTANT CTRL-F
$09 CONSTANT TAB
$0B CONSTANT CTRL-K
$0C CONSTANT CTRL-L
$17 CONSTANT CTRL-W
$19 CONSTANT CTRL-Y
$1B CONSTANT ESCAPE
$7F CONSTANT DELETE

\ Command type constants
0 CONSTANT NORMAL-COMMAND
1 CONSTANT KILL-FORWARD-COMMAND
2 CONSTANT KILL-BACKWARD-COMMAND
3 CONSTANT YANK-COMMAND

FORTH-WORDLIST DEQUE-WORDLIST LINE-WORDLIST 3 SET-ORDER
LINE-WORDLIST SET-CURRENT

TIB-SIZE CONSTANT LINE-HISTORY-DATA-SIZE

BEGIN-STRUCTURE LINE-HISTORY-HEADER-SIZE
  FIELD: LINE-HISTORY-ENTRY-LENGTH
END-STRUCTURE

LINE-HISTORY-HEADER-SIZE LINE-HISTORY-DATA-SIZE +
CONSTANT LINE-HISTORY-ITEM-SIZE

TIB-SIZE CONSTANT LINE-KILL-DATA-SIZE

BEGIN-STRUCTURE LINE-KILL-HEADER-SIZE
  FIELD: LINE-KILL-ENTRY-LENGTH
END-STRUCTURE

LINE-KILL-HEADER-SIZE LINE-KILL-DATA-SIZE +
CONSTANT LINE-KILL-ITEM-SIZE

128 CONSTANT LINE-HISTORY-SIZE
128 CONSTANT LINE-KILL-SIZE

BEGIN-STRUCTURE LINE-SIZE
  FIELD: LINE-OFFSET
  FIELD: LINE-HISTORY-DEQUE
  FIELD: LINE-HISTORY-EDITS-DEQUE
  FIELD: LINE-HISTORY-CURRENT
  FIELD: LINE-KILL-DEQUE
  FIELD: LINE-BUFFER
  FIELD: LINE-COUNT
  FIELD: LINE-START-ROW
  FIELD: LINE-START-COLUMN
  FIELD: LINE-TERMINAL-ROWS
  FIELD: LINE-TERMINAL-COLUMNS
  FIELD: LINE-SHOW-CURSOR-COUNT
  FIELD: LINE-LAST-COMMAND-TYPE
  FIELD: LINE-FIRST-LINE-ADDED
  FIELD: LINE-COMPLETE-ENABLE
END-STRUCTURE

\ Terminal structure for the current task
USER LINE

\ Unable to prepare a file descriptor
: X-UNABLE-TO-PREPARE-TERMINAL ( -- ) SPACE ." unable to prepare terminal" CR ;

\ Prepare for reading from a file descriptor
: (PREPARE-READ) ( fd -- )
  INPUT-FD @ = IF
    INPUT-FD @ PREPARE-TERMINAL AVERTS X-UNABLE-TO-PREPARE-TERMINAL
  THEN ;

\ Set the prepare read handler
' (PREPARE-READ) 'PREPARE-READ !

\ Initialize a line editor
: NEW-LINE ( -- editor )
  HERE LINE-SIZE ALLOT
  0 OVER LINE-OFFSET !
  LINE-HISTORY-SIZE LINE-HISTORY-ITEM-SIZE NEW-DEQUE OVER LINE-HISTORY-DEQUE !
  LINE-HISTORY-SIZE LINE-HISTORY-ITEM-SIZE NEW-DEQUE OVER
  LINE-HISTORY-EDITS-DEQUE !
  -1 OVER LINE-HISTORY-CURRENT !
  LINE-KILL-SIZE LINE-KILL-ITEM-SIZE NEW-DEQUE OVER LINE-KILL-DEQUE !
  HERE TIB-SIZE ALLOT OVER LINE-BUFFER !
  0 OVER LINE-COUNT !
  0 OVER LINE-START-ROW !
  0 OVER LINE-START-COLUMN !
  0 OVER LINE-TERMINAL-ROWS !
  0 OVER LINE-TERMINAL-COLUMNS !
  0 OVER LINE-SHOW-CURSOR-COUNT !
  NORMAL-COMMAND OVER LINE-LAST-COMMAND-TYPE !
  TRUE OVER LINE-COMPLETE-ENABLE ! ;

\ Set normal command type
: SET-NORMAL-COMMAND ( -- ) NORMAL-COMMAND LINE @ LINE-LAST-COMMAND-TYPE ! ;

\ Set kill forward command type
: SET-KILL-FORWARD-COMMAND ( -- )
  KILL-FORWARD-COMMAND LINE @ LINE-LAST-COMMAND-TYPE ! ;

\ Set kill backward command type
: SET-KILL-BACKWARD-COMMAND ( -- )
  KILL-BACKWARD-COMMAND LINE @ LINE-LAST-COMMAND-TYPE ! ;

\ Set yank command type
: SET-YANK-COMMAND ( -- ) YANK-COMMAND LINE @ LINE-LAST-COMMAND-TYPE ! ;

\ Type a decimal integer
: (DEC.) ( n -- ) 10 (BASE.) ;

\ Type the CSI sequence
: CSI ( -- ) ESCAPE EMIT [CHAR] [ EMIT ;

\ Show the cursor
: SHOW-CURSOR ( -- ) CSI [CHAR] ? EMIT 25 (DEC.) [CHAR] h EMIT ;

\ Hide the cursor
: HIDE-CURSOR ( -- ) CSI [CHAR] ? EMIT 25 (DEC.) [CHAR] l EMIT ;

\ Save the cursor position
: SAVE-CURSOR ( -- ) CSI [CHAR] s EMIT ;

\ Restore the cursor position
: RESTORE-CURSOR ( -- )  CSI [CHAR] u EMIT ;

\ Scroll up screen by one line
: SCROLL-UP ( lines -- ) CSI (DEC.) [CHAR] S EMIT ;

\ Move the cursor to specified row and column coordinates
: GO-TO-COORD ( row column -- )
  SWAP CSI 1+ (DEC.) [CHAR] ; EMIT 1+ (DEC.) [CHAR] f EMIT ;

\ Erase from the cursor to the end of the line
: ERASE-END-OF-LINE ( -- ) CSI [CHAR] K EMIT ;

\ Erase the lines below the current line
: ERASE-DOWN ( -- ) CSI [CHAR] J EMIT ;

\ Query for the cursor position
: QUERY-CURSOR-POSITION ( -- ) CSI [CHAR] 6 EMIT [CHAR] n EMIT ;

\ Show the cursor with a show/hide counter
: SHOW-CURSOR ( -- )
  1 LINE @ LINE-SHOW-CURSOR-COUNT +!
  LINE @ LINE-SHOW-CURSOR-COUNT @ 0 = IF SHOW-CURSOR THEN ;

\ Hide the cursor with a show/hide counter
: HIDE-CURSOR ( -- )
  -1 LINE @ LINE-SHOW-CURSOR-COUNT +!
  LINE @ LINE-SHOW-CURSOR-COUNT @ -1 = IF HIDE-CURSOR THEN ;

\ Execute code with a hidden cursor
: EXECUTE-HIDE-CURSOR ( xt -- ) HIDE-CURSOR TRY SHOW-CURSOR ?RAISE ;

\ Execute code while preserving cursor position
: EXECUTE-PRESERVE-CURSOR ( xt -- ) SAVE-CURSOR TRY RESTORE-CURSOR ?RAISE ;

\ Wait for a number
: WAIT-NUMBER ( -- n matches )
  HERE >R KEY DUP [CHAR] - = IF HERE C! 1 ALLOT ELSE SET-KEY THEN
  0 BEGIN
    DUP 64 < IF
      KEY DUP [CHAR] 0 >= OVER [CHAR] 9 <= AND IF
        HERE C! 1 ALLOT 1 + FALSE
      ELSE
        SET-KEY TRUE
      THEN
    ELSE
      TRUE
    THEN
  UNTIL
  DROP
  R@ HERE R@ - ['] PARSE-NUMBER 10 BASE-EXECUTE R> HERE! ;

\ Wait for a character
: WAIT-CHAR ( c -- ) BEGIN DUP KEY = UNTIL DROP ;

\ Confirm that a character is what is expected
: EXPECT-CHAR ( c -- flag ) KEY DUP ROT = IF DROP TRUE ELSE SET-KEY FALSE THEN ;

\ Get the cursor position
: GET-CURSOR-POSITION ( -- row column )
  BEGIN
    CLEAR-KEY QUERY-CURSOR-POSITION ESCAPE WAIT-CHAR
    [CHAR] [ EXPECT-CHAR IF
      WAIT-NUMBER IF
        1 - [CHAR] ; EXPECT-CHAR IF
	  WAIT-NUMBER IF
	    [CHAR] R EXPECT-CHAR IF
	      1 - TRUE
	    ELSE
	      2DROP FALSE
	    THEN
	  ELSE
	    DROP FALSE
	  THEN
	ELSE
	  DROP FALSE
	THEN
      ELSE
        DROP FALSE
      THEN
    ELSE
      FALSE
    THEN
  UNTIL ;

\ Get the cursor position with a hidden cursor
: GET-CURSOR-POSITION ( -- row column )
  ['] GET-CURSOR-POSITION EXECUTE-HIDE-CURSOR ;

\ Update the start position
: UPDATE-START-POSITION ( -- )
  GET-CURSOR-POSITION LINE @ LINE-START-COLUMN ! LINE @ LINE-START-ROW ! ;

\ Unable to get terminal size exception
: X-UNABLE-TO-GET-TERMINAL-SIZE ( -- )
  SPACE ." unable to get terminal size" CR ;

\ Update the terminal size
: UPDATE-TERMINAL-SIZE ( -- )
  INPUT-FD @ GET-TERMINAL-SIZE AVERTS X-UNABLE-TO-GET-TERMINAL-SIZE
  2DROP LINE @ LINE-TERMINAL-COLUMNS ! LINE @ LINE-TERMINAL-ROWS !
  LINE @ LINE-START-COLUMN @ LINE @ LINE-TERMINAL-COLUMNS @ >= IF
    0 LINE @ LINE-START-COLUMN ! 1 LINE @ LINE-START-ROW +!
    LINE @ LINE-START-ROW @ LINE @ LINE-TERMINAL-ROWS @ >= IF
      LINE @ LINE-TERMINAL-ROWS @ 1 - LINE @ LINE-START-ROW !
    THEN
  THEN ;

\ Get the start position
: START-POSITION ( -- row column )
  LINE @ LINE-START-ROW @ LINE @ LINE-START-COLUMN @ ;

\ Get the position at a character offset
: OFFSET-POSITION ( offset -- row column )
  LINE @ LINE-START-COLUMN @ +
  DUP LINE @ LINE-TERMINAL-COLUMNS @ / LINE @ LINE-START-ROW @ +
  SWAP LINE @ LINE-TERMINAL-COLUMNS @ MOD ;

\ Get the cursor position
: CURSOR-POSITION ( -- row column )
  LINE @ LINE-OFFSET @ OFFSET-POSITION ;

\ Get the end position
: END-POSITION ( -- row column ) LINE @ LINE-COUNT @ OFFSET-POSITION ;

\ Calculate number of lines text will take up
: TOTAL-LINES ( -- lines )
  LINE @ LINE-COUNT @ LINE @ LINE-START-COLUMN @ +
  LINE @ LINE-TERMINAL-COLUMNS @ / 1 + ;

\ Adjust start row
: ADJUST-START-ROW ( -- )
  LINE @ LINE-TERMINAL-ROWS @ TOTAL-LINES LINE @ LINE-START-ROW @ + - DUP 0 < IF
    DUP LINE @ LINE-START-ROW +! NEGATE SCROLL-UP
  ELSE
    DROP
  THEN ;

\ Actually update the line editor
: UPDATE-LINE ( -- )
  ADJUST-START-ROW
  START-POSITION GO-TO-COORD LINE @ LINE-BUFFER @ LINE @ LINE-COUNT @ TYPE
  END-POSITION GO-TO-COORD ERASE-DOWN ERASE-END-OF-LINE
  CURSOR-POSITION GO-TO-COORD ;

\ Update the line editor
: UPDATE-LINE ( -- ) ['] UPDATE-LINE EXECUTE-HIDE-CURSOR ;

\ Reset the line editor state for a new line of input
: RESET-LINE ( -- ) 0 LINE @ LINE-OFFSET ! 0 LINE @ LINE-COUNT ! ;

\ Actually configure the line editor state for a new line of input
: CONFIG-LINE ( -- )
  FALSE LINE @ LINE-FIRST-LINE-ADDED ! -1 LINE @ LINE-HISTORY-CURRENT !
  UPDATE-START-POSITION UPDATE-TERMINAL-SIZE ;

\ Configure the line editor state for a new line of input
: CONFIG-LINE ( -- ) ['] CONFIG-LINE EXECUTE-HIDE-CURSOR ;

\ Find the next non-whitespace character
: FIND-NEXT-PRINT ( offset1 -- offset2 )
  BEGIN
    DUP LINE @ LINE-COUNT @ < IF
      DUP LINE @ LINE-BUFFER @ + C@ DUP BL = SWAP TAB = OR NOT
      DUP NOT IF SWAP 1 + SWAP THEN
    ELSE
      TRUE
    THEN
  UNTIL ;

\ Find the next whitespace character
: FIND-NEXT-WS ( offset1 -- offset2 )
  BEGIN
    DUP LINE @ LINE-COUNT @ < IF
      DUP LINE @ LINE-BUFFER @ + C@ DUP BL = SWAP TAB = OR
      DUP NOT IF SWAP 1 + SWAP THEN
    ELSE
      TRUE
    THEN
  UNTIL ;

\ Find the previous non-whitespace character
: FIND-PREV-PRINT ( offset1 -- offset2 )
  BEGIN
    DUP 0 > IF
      DUP LINE @ LINE-BUFFER @ + C@ DUP BL = SWAP TAB = OR NOT
      DUP NOT IF SWAP 1 - SWAP THEN
    ELSE
      TRUE
    THEN
  UNTIL ;

\ Find the previous whitespace character
: FIND-PREV-WS ( offset1 -- offset2 )
  BEGIN
    DUP 0 > IF
      DUP LINE @ LINE-BUFFER @ + C@ DUP BL = SWAP TAB = OR
      DUP NOT IF SWAP 1 - SWAP THEN
    ELSE
      TRUE
    THEN
  UNTIL ;

\ Find the last previous non-whitespace character
: FIND-PREV-LAST-PRINT ( offset1 -- offset2 )
  BEGIN
    DUP 0 > IF
      DUP 1 - LINE @ LINE-BUFFER @ + C@ DUP BL = SWAP TAB = OR
      DUP NOT IF SWAP 1 - SWAP THEN
    ELSE
      TRUE
    THEN
  UNTIL ;

\ Find the last previous whitespace character
: FIND-PREV-LAST-WS ( offset1 -- offset2 )
  BEGIN
    DUP 0 > IF
      DUP 1 - LINE @ LINE-BUFFER @ + C@ DUP BL = SWAP TAB = OR NOT
      DUP NOT IF SWAP 1 - SWAP THEN
    ELSE
      TRUE
    THEN
  UNTIL ;

\ Generic history exception
: X-HISTORY-INTERNAL ( -- ) SPACE ." internal history failure" CR ;

\ Generic kill exception
: X-KILL-INTERNAL ( -- ) SPACE ." internal kill failure" CR ;

\ Actually add a line to history
: ADD-TO-HISTORY ( -- )
  LINE @ LINE-HISTORY-DEQUE @ COUNT-DEQUE LINE-HISTORY-SIZE = IF
    LINE @ LINE-HISTORY-DEQUE @ DROP-END-DEQUE AVERTS X-HISTORY-INTERNAL
    LINE @ LINE-HISTORY-EDITS-DEQUE @ DROP-END-DEQUE AVERTS X-HISTORY-INTERNAL
  THEN
  HERE LINE @ LINE-COUNT @ OVER LINE-HISTORY-ENTRY-LENGTH !
  LINE @ LINE-BUFFER @ OVER LINE-HISTORY-HEADER-SIZE + TIB-SIZE MOVE
  LINE-HISTORY-ITEM-SIZE ALLOT
  DUP LINE @ LINE-HISTORY-DEQUE @ PUSH-START-DEQUE AVERTS X-HISTORY-INTERNAL
  LINE @ LINE-HISTORY-EDITS-DEQUE @ PUSH-START-DEQUE AVERTS X-HISTORY-INTERNAL
  LINE-HISTORY-ITEM-SIZE NEGATE ALLOT ;

\ Set added first line
: SET-ADDED-FIRST-LINE-IN-HISTORY ( -- )
  HERE LINE-HISTORY-ITEM-SIZE ALLOT
  LINE @ LINE-BUFFER @ OVER LINE-HISTORY-HEADER-SIZE +
  LINE @ LINE-COUNT @ MOVE LINE @ LINE-COUNT @ OVER !
  0 LINE @ LINE-HISTORY-DEQUE @ SET-DEQUE LINE-HISTORY-ITEM-SIZE NEGATE ALLOT ;

\ Compare and add first line
: COMPARE-ADD-TO-HISTORY ( -- )
  HERE LINE-HISTORY-ITEM-SIZE ALLOT
  DUP 0 LINE @ LINE-HISTORY-DEQUE @ GET-DEQUE AVERTS X-HISTORY-INTERNAL
  DUP LINE-HISTORY-HEADER-SIZE + SWAP LINE-HISTORY-ENTRY-LENGTH @
  LINE @ LINE-BUFFER @ LINE @ LINE-COUNT @ EQUAL-STRINGS?
  LINE-HISTORY-ITEM-SIZE NEGATE ALLOT NOT IF ADD-TO-HISTORY THEN ;

\ Remove the first line if empty
: REMOVE-FIRST-LINE-IF-EMPTY ( -- )
  HERE LINE-HISTORY-ITEM-SIZE ALLOT
  DUP 0 LINE @ LINE-HISTORY-DEQUE @ GET-DEQUE AVERTS X-HISTORY-INTERNAL
  LINE-HISTORY-ENTRY-LENGTH @ 0 = IF
    FALSE LINE @ LINE-FIRST-LINE-ADDED !
    LINE @ LINE-HISTORY-DEQUE @ DROP-START-DEQUE AVERTS X-HISTORY-INTERNAL
  THEN
  LINE-HISTORY-ITEM-SIZE NEGATE ALLOT ;

\ Add a line to history
: ADD-TO-HISTORY ( -- )
  LINE @ LINE-FIRST-LINE-ADDED @
  LINE @ LINE-HISTORY-DEQUE @ COUNT-DEQUE 1 > AND IF
    REMOVE-FIRST-LINE-IF-EMPTY
  THEN
  LINE @ LINE-COUNT @ 0 > IF
    LINE @ LINE-FIRST-LINE-ADDED @ LINE @ LINE-HISTORY-CURRENT @ 0 = AND IF
      SET-ADDED-FIRST-LINE-IN-HISTORY
    ELSE LINE @ LINE-HISTORY-DEQUE @ COUNT-DEQUE 0 > IF
      COMPARE-ADD-TO-HISTORY
    ELSE
      ADD-TO-HISTORY
    THEN THEN
  THEN ;

\ Revert history edits
: REVERT-HISTORY-EDITS ( -- )
  LINE @ LINE-HISTORY-DEQUE @ COUNT-DEQUE 0 ?DO
    HERE LINE-HISTORY-ITEM-SIZE ALLOT
    DUP I LINE @ LINE-HISTORY-DEQUE @ GET-DEQUE AVERTS X-HISTORY-INTERNAL
    I LINE @ LINE-HISTORY-EDITS-DEQUE @ SET-DEQUE AVERTS X-HISTORY-INTERNAL
    LINE-HISTORY-ITEM-SIZE NEGATE ALLOT
  LOOP ;

\ Handle newlines
: HANDLE-NEWLINE ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-COUNT @ LINE @ LINE-OFFSET ! UPDATE-LINE SPACE
  ADD-TO-HISTORY REVERT-HISTORY-EDITS ;

\ Handle delete
: HANDLE-DELETE ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-OFFSET @ 0 > IF
    LINE @ LINE-BUFFER @ LINE @ LINE-OFFSET @ + DUP 1 -
    LINE @ LINE-COUNT @ LINE @ LINE-OFFSET @ - MOVE
    -1 LINE @ LINE-OFFSET +! -1 LINE @ LINE-COUNT +!
  THEN
  UPDATE-LINE ;

\ Handle delete forward
: HANDLE-DELETE-FORWARD ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-OFFSET @ LINE @ LINE-COUNT @ < IF
    LINE @ LINE-BUFFER @ LINE @ LINE-OFFSET @ + DUP 1 + SWAP
    LINE @ LINE-COUNT @ LINE @ LINE-OFFSET @ - 1 - MOVE
    -1 LINE @ LINE-COUNT +!
  THEN
  UPDATE-LINE ;

\ Move first line into history deque
: MOVE-INTO-HISTORY ( -- )
  HERE LINE-HISTORY-ITEM-SIZE ALLOT
  LINE @ LINE-BUFFER @ OVER LINE-HISTORY-HEADER-SIZE +
  LINE @ LINE-COUNT @ MOVE
  LINE @ LINE-COUNT @ OVER LINE-HISTORY-ENTRY-LENGTH !
  DUP LINE @ LINE-HISTORY-DEQUE @ PUSH-START-DEQUE AVERTS X-HISTORY-INTERNAL
  LINE @ LINE-HISTORY-EDITS-DEQUE @ PUSH-START-DEQUE AVERTS X-HISTORY-INTERNAL
  LINE-HISTORY-ITEM-SIZE NEGATE ALLOT
  0 LINE @ LINE-HISTORY-CURRENT ! TRUE LINE @ LINE-FIRST-LINE-ADDED ! ;

\ Save current line into history edits
: SAVE-CURRENT-EDIT ( -- )
  HERE LINE-HISTORY-ITEM-SIZE ALLOT
  LINE @ LINE-BUFFER @ OVER LINE-HISTORY-HEADER-SIZE +
  LINE @ LINE-COUNT @ MOVE
  LINE @ LINE-COUNT @ OVER LINE-HISTORY-ENTRY-LENGTH !
  LINE @ LINE-HISTORY-CURRENT @
  LINE @ LINE-HISTORY-EDITS-DEQUE @ SET-DEQUE AVERTS X-HISTORY-INTERNAL
  LINE-HISTORY-ITEM-SIZE NEGATE ALLOT ;

\ Read current line from history edits
: READ-CURRENT-EDIT ( -- )
  LINE @ LINE-HISTORY-CURRENT @ 0 >= IF
    HERE LINE-HISTORY-ITEM-SIZE ALLOT
    DUP LINE @ LINE-HISTORY-CURRENT @
    LINE @ LINE-HISTORY-EDITS-DEQUE @ GET-DEQUE AVERTS X-HISTORY-INTERNAL
    DUP LINE-HISTORY-ENTRY-LENGTH @ LINE @ LINE-COUNT !
    LINE-HISTORY-HEADER-SIZE + LINE @ LINE-BUFFER @ LINE @ LINE-COUNT @ MOVE
    LINE-HISTORY-ITEM-SIZE NEGATE ALLOT
  THEN ;

\ Handle up key
: HANDLE-UP ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-HISTORY-CURRENT @ LINE @ LINE-HISTORY-DEQUE @ COUNT-DEQUE 1 - < IF
    LINE @ LINE-HISTORY-CURRENT @ -1 = IF
      MOVE-INTO-HISTORY
    ELSE
      SAVE-CURRENT-EDIT
    THEN
    1 LINE @ LINE-HISTORY-CURRENT +! READ-CURRENT-EDIT 0 LINE @ LINE-OFFSET !
  THEN
  UPDATE-LINE ;

\ Handle down key
: HANDLE-DOWN ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-HISTORY-CURRENT @ 0 > IF
    SAVE-CURRENT-EDIT -1 LINE @ LINE-HISTORY-CURRENT +!
    READ-CURRENT-EDIT 0 LINE @ LINE-OFFSET !
  THEN
  UPDATE-LINE ;

\ Handle go to start
: HANDLE-START ( -- ) SET-NORMAL-COMMAND 0 LINE @ LINE-OFFSET ! UPDATE-LINE ;

\ Handle go to end
: HANDLE-END ( -- )
  SET-NORMAL-COMMAND LINE @ LINE-COUNT @ LINE @ LINE-OFFSET ! UPDATE-LINE ;

\ Handle bye
: HANDLE-BYE ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-COUNT @ LINE @ LINE-OFFSET ! UPDATE-LINE CR BYE ;

\ Handle go forward
: HANDLE-FORWARD ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-OFFSET @ 1 + LINE @ LINE-COUNT @ MIN LINE @ LINE-OFFSET !
  UPDATE-LINE ;

\ Handle go backward
: HANDLE-BACKWARD ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-OFFSET @ 1 - 0 MAX LINE @ LINE-OFFSET ! UPDATE-LINE ;

\ Handle go forward one word
: HANDLE-FORWARD-WORD ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-OFFSET @ FIND-NEXT-PRINT FIND-NEXT-WS LINE @ LINE-OFFSET !
  UPDATE-LINE ;

\ Handle go backward one word
: HANDLE-BACKWARD-WORD ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-OFFSET @ FIND-PREV-LAST-WS FIND-PREV-LAST-PRINT
  LINE @ LINE-OFFSET ! UPDATE-LINE ;

\ Free space if necessary in the kill deque
: FREE-KILL-DEQUE-SPACE ( -- )
  LINE @ LINE-KILL-DEQUE @ COUNT-DEQUE
  LINE @ LINE-KILL-DEQUE @ GET-DEQUE-MAX-COUNT = IF
    LINE @ LINE-KILL-DEQUE @ DROP-END-DEQUE AVERTS X-KILL-INTERNAL
  THEN ;

\ Add a new kill with text at the given offset and length
: ADD-NEW-KILL ( offset length -- )
  SWAP >R >R FREE-KILL-DEQUE-SPACE
  HERE LINE-KILL-ITEM-SIZE ALLOT
  R> 2DUP SWAP LINE-KILL-ENTRY-LENGTH ! LINE @ LINE-BUFFER @ R> +
  2 PICK LINE-KILL-HEADER-SIZE + ROT MOVE
  LINE @ LINE-KILL-DEQUE @ PUSH-START-DEQUE AVERTS X-KILL-INTERNAL
  LINE-KILL-ITEM-SIZE NEGATE ALLOT ;

\ Basic case of killing forward to the end of the line
: HANDLE-KILL-FORWARD-LINE-BASIC ( -- )
  LINE @ LINE-OFFSET @ LINE @ LINE-COUNT @ LINE @ LINE-OFFSET @ - ADD-NEW-KILL
  LINE @ LINE-OFFSET @ LINE @ LINE-COUNT ! ;

\ Add text at the given offset and length to the start of the most recent kill
: ADD-TEXT-TO-KILL-START ( offset length -- )
  SWAP >R >R HERE LINE-KILL-ITEM-SIZE ALLOT
  DUP 0 LINE @ LINE-KILL-DEQUE @ GET-DEQUE AVERTS X-KILL-INTERNAL
  DUP LINE-KILL-HEADER-SIZE + OVER LINE-KILL-HEADER-SIZE + R@ +
  2 PICK LINE-KILL-ENTRY-LENGTH @ MOVE
  R@ OVER LINE-KILL-ENTRY-LENGTH +!
  R> LINE @ LINE-BUFFER @ R> + 2 PICK LINE-KILL-HEADER-SIZE + ROT MOVE
  0 LINE @ LINE-KILL-DEQUE @ SET-DEQUE AVERTS X-KILL-INTERNAL
  LINE-KILL-ITEM-SIZE NEGATE ALLOT ;

\ Add text at the given offset and length to the end of the most recent kill
: ADD-TEXT-TO-KILL-END ( offset length -- )
  >R >R HERE LINE-KILL-ITEM-SIZE ALLOT
  DUP 0 LINE @ LINE-KILL-DEQUE @ GET-DEQUE AVERTS X-KILL-INTERNAL
  LINE @ LINE-BUFFER @ R> +
  OVER LINE-KILL-HEADER-SIZE + 2 PICK LINE-KILL-ENTRY-LENGTH @ +
  R@ MOVE R> OVER LINE-KILL-ENTRY-LENGTH +!
  0 LINE @ LINE-KILL-DEQUE @ SET-DEQUE AVERTS X-KILL-INTERNAL
  LINE-KILL-ITEM-SIZE NEGATE ALLOT ;

\ Case of killing forward to the end of the line and adding the text to the end
\ of the most recent kill
: HANDLE-KILL-FORWARD-LINE-ADD ( -- )
  LINE @ LINE-OFFSET @
  LINE @ LINE-COUNT @ LINE @ LINE-OFFSET @ - ADD-TEXT-TO-KILL-END ;

\ Check whether there is room for a given length of text in the most recent kill
: CHECK-ROOM-FOR-KILL ( length -- flag )
  >R HERE LINE-KILL-ITEM-SIZE ALLOT
  DUP 0 LINE @ LINE-KILL-DEQUE @ GET-DEQUE AVERTS X-KILL-INTERNAL
  LINE-KILL-DATA-SIZE SWAP LINE-KILL-ENTRY-LENGTH @ - R> >=
  LINE-KILL-ITEM-SIZE NEGATE ALLOT ;

\ Handle kill forward to the end of the line
: HANDLE-KILL-FORWARD-LINE ( -- )
  LINE @ LINE-OFFSET @ LINE @ LINE-COUNT @ < IF
    LINE @ LINE-LAST-COMMAND-TYPE @ KILL-FORWARD-COMMAND =
    SET-KILL-FORWARD-COMMAND
    LINE @ LINE-KILL-DEQUE @ COUNT-DEQUE 0 > AND IF
      LINE @ LINE-COUNT @ LINE @ LINE-OFFSET @ CHECK-ROOM-FOR-KILL IF
        HANDLE-KILL-FORWARD-LINE-ADD
      ELSE
        HANDLE-KILL-FORWARD-LINE-BASIC
      THEN
    ELSE
      HANDLE-KILL-FORWARD-LINE-BASIC
    THEN
  ELSE
    SET-KILL-FORWARD-COMMAND
  THEN
  UPDATE-LINE ;

\ Remove text from the input buffer
: REMOVE-TEXT ( offset length -- )
  2DUP + LINE @ LINE-BUFFER @ + 2 PICK LINE @ LINE-BUFFER @ +
  3 ROLL 3 PICK + LINE @ LINE-COUNT @ SWAP - MOVE
  NEGATE LINE @ LINE-COUNT +! ;

\ Insert text into the input buffer
: INSERT-TEXT ( addr length offset -- )
  LINE @ LINE-BUFFER @ OVER + LINE @ LINE-BUFFER @ 2 PICK + 3 PICK +
  LINE @ LINE-COUNT @ 3 PICK - MOVE
  ROT LINE @ LINE-BUFFER @ ROT + 2 PICK MOVE LINE @ LINE-COUNT +! ;

\ Basic case of killing forward to the start of the next whitespace
: HANDLE-KILL-TO-NEXT-SPACE-BASIC ( -- )
  LINE @ LINE-OFFSET @ FIND-NEXT-PRINT FIND-NEXT-WS LINE @ LINE-OFFSET @ -
  DUP 0 > IF
    LINE @ LINE-OFFSET @ SWAP 2DUP ADD-NEW-KILL REMOVE-TEXT
  ELSE
    DROP
  THEN ;

\ Case of killing forward to the start of the next whitespace and adding the
\ text to the end of the most recent kill
: HANDLE-KILL-TO-NEXT-SPACE-ADD ( -- )
  LINE @ LINE-OFFSET @ FIND-NEXT-PRINT FIND-NEXT-WS LINE @ LINE-OFFSET @ -
  DUP 0 > IF
    DUP CHECK-ROOM-FOR-KILL IF
      LINE @ LINE-OFFSET @ SWAP 2DUP ADD-TEXT-TO-KILL-END REMOVE-TEXT
    ELSE
      LINE @ LINE-OFFSET @ SWAP 2DUP ADD-NEW-KILL REMOVE-TEXT
    THEN
  ELSE
    DROP
  THEN ;

\ Handle kill forward to the end of the next word
: HANDLE-KILL-TO-NEXT-SPACE ( -- )
  LINE @ LINE-OFFSET @ LINE @ LINE-COUNT @ < IF
    LINE @ LINE-LAST-COMMAND-TYPE @ KILL-FORWARD-COMMAND =
    SET-KILL-FORWARD-COMMAND
    LINE @ LINE-KILL-DEQUE @ COUNT-DEQUE 0 > AND IF
      HANDLE-KILL-TO-NEXT-SPACE-ADD
    ELSE
      HANDLE-KILL-TO-NEXT-SPACE-BASIC
    THEN
  ELSE
    SET-KILL-FORWARD-COMMAND
  THEN
  UPDATE-LINE ;

\ Basic case of killing backward to the start of the previous word
: HANDLE-KILL-TO-PREV-SPACE-BASIC ( -- )
  LINE @ LINE-OFFSET @ FIND-PREV-LAST-WS FIND-PREV-LAST-PRINT
  DUP LINE @ LINE-OFFSET @ < IF
    LINE @ LINE-OFFSET @ OVER - DUP >R 2DUP ADD-NEW-KILL REMOVE-TEXT
    R> NEGATE LINE @ LINE-OFFSET +!
  ELSE
    DROP
  THEN ;

\ Case of killing backward to the start of the previous word and adding the
\ text to the start of the most recent kill
\ Basic case of killing forward to the start of the previous word
: HANDLE-KILL-TO-PREV-SPACE-ADD ( -- )
  LINE @ LINE-OFFSET @ FIND-PREV-LAST-WS FIND-PREV-LAST-PRINT
  DUP LINE @ LINE-OFFSET @ < IF
    LINE @ LINE-OFFSET @ OVER - DUP >R DUP CHECK-ROOM-FOR-KILL IF
      2DUP ADD-TEXT-TO-KILL-START REMOVE-TEXT
    ELSE
      2DUP ADD-NEW-KILL REMOVE-TEXT
    THEN
    R> NEGATE LINE @ LINE-OFFSET +!
  ELSE
    DROP
  THEN ;

\ Handle kill backward to the start of the previous word
: HANDLE-KILL-TO-PREV-SPACE ( -- )
  LINE @ LINE-OFFSET @ 0 > IF
    LINE @ LINE-LAST-COMMAND-TYPE @ KILL-BACKWARD-COMMAND =
    SET-KILL-BACKWARD-COMMAND
    LINE @ LINE-KILL-DEQUE @ COUNT-DEQUE 0 > AND IF
      HANDLE-KILL-TO-PREV-SPACE-ADD
    ELSE
      HANDLE-KILL-TO-PREV-SPACE-BASIC
    THEN
  ELSE
    SET-KILL-BACKWARD-COMMAND
  THEN
  UPDATE-LINE ;

\ Insert yanked text
: INSERT-YANK ( -- )
  HERE LINE-KILL-ITEM-SIZE ALLOT
  DUP 0 LINE @ LINE-KILL-DEQUE @ GET-DEQUE AVERTS X-KILL-INTERNAL
  DUP LINE-KILL-HEADER-SIZE +
  TIB-SIZE LINE @ LINE-COUNT @ - ROT LINE-KILL-ENTRY-LENGTH @ MIN DUP >R
  LINE @ LINE-OFFSET @ INSERT-TEXT R> LINE @ LINE-OFFSET +!
  LINE-KILL-ITEM-SIZE NEGATE ALLOT ;

\ Remove yanked text
: REMOVE-YANK ( -- )
  HERE LINE-KILL-ITEM-SIZE ALLOT
  DUP 0 LINE @ LINE-KILL-DEQUE @ GET-DEQUE AVERTS X-KILL-INTERNAL
  DUP LINE-KILL-ENTRY-LENGTH @ LINE @ LINE-OFFSET @ OVER - SWAP REMOVE-TEXT
  LINE-KILL-ENTRY-LENGTH @ NEGATE LINE @ LINE-OFFSET +!
  LINE-KILL-ITEM-SIZE NEGATE ALLOT ;

\ Cycle through yanked text
: CYCLE-YANK ( -- )
  HERE LINE-KILL-ITEM-SIZE ALLOT
  DUP LINE @ LINE-KILL-DEQUE @ POP-START-DEQUE AVERTS X-KILL-INTERNAL
  LINE @ LINE-KILL-DEQUE @ PUSH-END-DEQUE AVERTS X-KILL-INTERNAL
  LINE-KILL-ITEM-SIZE NEGATE ALLOT ;

\ Handle yanking the most recent kill
: HANDLE-YANK ( -- )
  SET-YANK-COMMAND
  LINE @ LINE-KILL-DEQUE @ COUNT-DEQUE 0 > IF
    INSERT-YANK
  THEN
  UPDATE-LINE ;

\ Handle yanking the previous kill
: HANDLE-YANK-PREV ( -- )
  LINE @ LINE-LAST-COMMAND-TYPE @ YANK-COMMAND = IF
    SET-YANK-COMMAND
    LINE @ LINE-KILL-DEQUE @ COUNT-DEQUE 1 > IF
      REMOVE-YANK CYCLE-YANK INSERT-YANK
    THEN
    UPDATE-LINE
  ELSE
    HANDLE-YANK
  THEN ;

\ Test whether a word is an initial match for a string
: WORD-PREFIX? ( c-addr u xt -- matches )
  WORD>NAME 2 PICK SWAP <= IF
    SWAP EQUAL-CASE-CHARS?
  ELSE
    DROP 2DROP FALSE
  THEN ;

\ Get whether a completion is actually unique for a wordlist
: WORDLIST-REST-COMPLETION? ( c-addr u xt -- xt duplicate )
  DUP WORD>NEXT BEGIN
    DUP 0 <> IF
      3 PICK 3 PICK 2 PICK WORD-PREFIX? IF
        2DROP 2DROP 0 TRUE TRUE
      ELSE
        WORD>NEXT FALSE
      THEN
    ELSE
      DROP ROT ROT 2DROP FALSE TRUE
    THEN
  UNTIL ;

\ Get whether a completion is unique for a wordlist
: WORDLIST-COMPLETION? ( c-addr u wid -- xt duplicate )
  WORDLIST>FIRST BEGIN
    DUP 0 <> IF
      2 PICK 2 PICK 2 PICK WORD-PREFIX? IF
        WORDLIST-REST-COMPLETION? TRUE
      ELSE
        WORD>NEXT FALSE
      THEN
    ELSE
      DROP 2DROP 0 FALSE TRUE
    THEN
  UNTIL ;

\ Test whether there are no more potential completions
: REST-COMPLETION? ( x*u2 c-addr u1 u2 xt -- xt duplicate )
  BEGIN
    OVER 0 > IF
      SWAP 1- SWAP 3 PICK 3 PICK 6 ROLL WORDLIST-COMPLETION? SWAP 0 <> AND IF
        DROP 2 + DROPS 0 TRUE TRUE
      ELSE
	FALSE
      THEN
    ELSE
      NIP NIP NIP FALSE TRUE
    THEN
  UNTIL ;

\ Test whether a given prefix has only one possible completion
: ONLY-COMPLETION? ( c-addr u -- xt duplicate )
  2>R GET-ORDER 2R> ROT BEGIN
    DUP 0 > IF
      1 - 2 PICK 2 PICK 5 ROLL WORDLIST-COMPLETION? IF
        DROP 2 + DROPS 0 TRUE TRUE
      ELSE DUP 0 <> IF
        REST-COMPLETION? TRUE
      ELSE
        DROP FALSE
      THEN THEN
    ELSE
      DROP 2DROP 0 FALSE TRUE
    THEN
  UNTIL ;

\ Display available completions for a single wordlist
: DISPLAY-WORDLIST-COMPLETIONS ( c-addr u wid -- )
  WORDLIST>FIRST BEGIN
    DUP 0 <> IF
      2 PICK 2 PICK 2 PICK WORD-PREFIX? IF
        DUP WORD>NAME TYPE SPACE
      THEN
      WORD>NEXT FALSE
    ELSE
      DROP 2DROP TRUE
    THEN
  UNTIL ;

\ Display available completions for all wordlists
: DISPLAY-COMPLETIONS ( c-addr u -- )
  CR
  >R >R GET-ORDER R> R> ROT BEGIN
    DUP 0 > IF
      1 - 2 PICK 2 PICK 5 ROLL DISPLAY-WORDLIST-COMPLETIONS FALSE
    ELSE
      DROP 2DROP TRUE
    THEN
  UNTIL
  CR
  CONFIG-LINE UPDATE-LINE ;

\ Insert a completion
: INSERT-COMPLETION ( c-addr u -- )
  LINE @ LINE-OFFSET @ FIND-PREV-LAST-PRINT DUP LINE @ LINE-OFFSET @ OVER -
  REMOVE-TEXT 2DUP + LINE @ LINE-OFFSET ! INSERT-TEXT ;  

\ Handle autocomplete
: HANDLE-TAB ( -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-COMPLETE-ENABLE @ IF
    LINE @ LINE-OFFSET @ FIND-PREV-LAST-PRINT DUP LINE @ LINE-OFFSET @ < IF
      LINE @ LINE-BUFFER @ OVER + LINE @ LINE-OFFSET @ ROT -
      2DUP ONLY-COMPLETION? NOT IF
        ?DUP IF WORD>NAME INSERT-COMPLETION THEN 2DROP
      ELSE
        DROP DISPLAY-COMPLETIONS
      THEN
    ELSE
      DROP
    THEN
  THEN
  UPDATE-LINE ;

\ Refresh
: HANDLE-REFRESH ( -- ) UPDATE-LINE ;

\ Handle a normal key
: HANDLE-NORMAL-KEY ( c -- )
  SET-NORMAL-COMMAND
  LINE @ LINE-COUNT @ TIB-SIZE < IF
    LINE @ LINE-BUFFER @ LINE @ LINE-OFFSET @ + DUP 1 +
    LINE @ LINE-COUNT @ LINE @ LINE-OFFSET @ - MOVE
    LINE @ LINE-BUFFER @ LINE @ LINE-OFFSET @ + C! 1 LINE @ LINE-OFFSET +!
    1 LINE @ LINE-COUNT +!
  ELSE
    DROP
  THEN
  UPDATE-LINE ;

\ Handle special control keys; returns whether to stop reading
: HANDLE-CTRL-SPECIAL ( source -- stop )
  KEY UPDATE-TERMINAL-SIZE CASE
    [CHAR] ; OF
      KEY UPDATE-TERMINAL-SIZE CASE
        [CHAR] 5 OF
          KEY UPDATE-TERMINAL-SIZE CASE
            [CHAR] C OF HANDLE-FORWARD-WORD FALSE ENDOF
            [CHAR] D OF HANDLE-BACKWARD-WORD FALSE ENDOF
	    DUP >R SET-KEY FALSE R>
          ENDCASE
        ENDOF
	DUP >R SET-KEY FALSE R>
      ENDCASE
    ENDOF
    DUP >R SET-KEY FALSE R>
  ENDCASE ;

\ Handle special keys; returns whether to stop reading
: HANDLE-SPECIAL ( source -- stop )
  KEY UPDATE-TERMINAL-SIZE CASE
    [CHAR] C OF HANDLE-FORWARD FALSE ENDOF
    [CHAR] D OF HANDLE-BACKWARD FALSE ENDOF
    [CHAR] A OF HANDLE-UP FALSE ENDOF
    [CHAR] B OF HANDLE-DOWN FALSE ENDOF
    [CHAR] 3 OF
      KEY UPDATE-TERMINAL-SIZE CASE
        [CHAR] ~ OF HANDLE-DELETE-FORWARD FALSE ENDOF
	DUP >R SET-KEY FALSE R>
      ENDCASE
    ENDOF
    [CHAR] 1 OF HANDLE-CTRL-SPECIAL ENDOF
    DUP >R SET-KEY FALSE R>
  ENDCASE ;

\ Handle keypresses including escape characters; returns whether to stop reading
: HANDLE-ESCAPE ( source -- stop )
  KEY UPDATE-TERMINAL-SIZE CASE
    [CHAR] [ OF HANDLE-SPECIAL ENDOF
    [CHAR] f OF HANDLE-FORWARD-WORD FALSE ENDOF
    [CHAR] b OF HANDLE-BACKWARD-WORD FALSE ENDOF
    [CHAR] y OF HANDLE-YANK-PREV FALSE ENDOF
    [CHAR] d OF HANDLE-KILL-TO-NEXT-SPACE FALSE ENDOF
    DELETE OF HANDLE-KILL-TO-PREV-SPACE FALSE ENDOF
    DUP >R SET-KEY FALSE R>
  ENDCASE ;

\ Handle keypresses; returns whether to stop reading
: HANDLE-KEY ( -- stop )
  KEY UPDATE-TERMINAL-SIZE CASE
    NEWLINE OF HANDLE-NEWLINE TRUE ENDOF
    DELETE OF HANDLE-DELETE FALSE ENDOF
    TAB OF HANDLE-TAB FALSE ENDOF
    ESCAPE OF HANDLE-ESCAPE ENDOF
    CTRL-A OF HANDLE-START FALSE ENDOF
    CTRL-D OF HANDLE-BYE FALSE ENDOF \ HANDLE-BYE should never return
    CTRL-E OF HANDLE-END FALSE ENDOF
    CTRL-F OF HANDLE-FORWARD FALSE ENDOF
    CTRL-B OF HANDLE-BACKWARD FALSE ENDOF
    CTRL-K OF HANDLE-KILL-FORWARD-LINE FALSE ENDOF
    CTRL-L OF HANDLE-REFRESH FALSE ENDOF
    CTRL-W OF HANDLE-KILL-TO-PREV-SPACE FALSE ENDOF
    CTRL-Y OF HANDLE-YANK FALSE ENDOF
    DUP $20 < IF FALSE SWAP ELSE DUP HANDLE-NORMAL-KEY FALSE SWAP THEN
  ENDCASE ;

\ Edit a line of text
: EDIT-LINE ( -- ) RESET-LINE CONFIG-LINE BEGIN HANDLE-KEY UNTIL ;

\ Read a line of text
: (ACCEPT) ( addr bytes1 -- bytes2 )
  EDIT-LINE LINE @ LINE-COUNT @ MIN LINE @ LINE-BUFFER @ SWAP ROT SWAP
  DUP >R CMOVE R> ;

\ Create a line editor for the main task
NEW-LINE LINE !

\ Set accept hook
' (ACCEPT) 'ACCEPT !

BASE ! SET-CURRENT SET-ORDER

