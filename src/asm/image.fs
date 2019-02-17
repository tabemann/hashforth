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

\ TRUE constant macro
: +TRUE -1 VM LIT NOT-VM ;

\ FALSE constant macro
: +FALSE 0 VM LIT NOT-VM ;

VM

\ TRUE constant
DEFINE-WORD TRUE +TRUE END-WORD

\ FALSE constant
DEFINE-WORD FALSE +FALSE END-WORD

\ Cell size in bytes constant
DEFINE-WORD CELL-SIZE ( -- u ) TARGET-CELL-SIZE LIT END-WORD

\ Advance a pointer by one cell
DEFINE-WORD CELL+ ( u -- u ) TARGET-CELL-SIZE LIT + END-WORD

\ Multiply a value by the size of a cell
DEFINE-WORD CELLS ( u -- u ) TARGET-CELL-SIZE LIT * END-WORD

\ Small token size in bytes constant
DEFINE-WORD HALF-TOKEN-SIZE ( -- u ) TARGET-HALF-TOKEN-SIZE LIT END-WORD

\ Full token size in bytes constant
DEFINE-WORD FULL-TOKEN-SIZE ( -- u ) TARGET-FULL-TOKEN-SIZE LIT END-WORD

\ Get whether tokens use a token size bit
DEFINE-WORD TOKEN-FLAG-BIT ( -- flag )
  HALF-TOKEN-SIZE FULL-TOKEN-SIZE <>
END-WORD

\ The HERE pointer is stored here
DEFINE-WORD-CREATED USER-SPACE-CURRENT
0 SET-CELL-DATA

\ Get the HERE pointer
DEFINE-WORD HERE ( -- addr ) USER-SPACE-CURRENT @ END-WORD

\ Set the HERE pointer
DEFINE-WORD HERE! ( addr -- ) USER-SPACE-CURRENT ! END-WORD

\ Advance or, with negative values, retract the HERE pointer by a number of
\ bytes
DEFINE-WORD ALLOT ( u -- ) HERE + HERE! END-WORD

\ Negate a value
DEFINE-WORD NEGATE ( n -- n ) 1 LIT - NOT END-WORD

\ Duplicate the value under the top of the stack
DEFINE-WORD OVER ( x1 x2 -- x1 x2 x1 ) 1 LIT PICK END-WORD

\ Drop the value under the top of the stack
DEFINE-WORD NIP ( x1 x2 -- x2 ) SWAP DROP END-WORD

\ Insert the value at the top of the stack under the value under the top of
\ the stack
DEFINE-WORD TUCK ( x1 x2 -- x2 x1 x2 ) SWAP OVER END-WORD

\ Duplicate the two values on the top of the stack, preserving their order
DEFINE-WORD 2DUP ( x1 x2 -- x1 x2 x1 x2 ) OVER OVER END-WORD

\ Drop the two values on the top of the stack
DEFINE-WORD 2DROP ( x1 x2 -- ) DROP DROP END-WORD

\ Get whether one value is smaller than (signed) or equal to another value
DEFINE-WORD <= ( n1 n2 -- flag ) 2DUP = ROT ROT < OR END-WORD

\ Get whether one value is greater than (signed) or equal to another value
DEFINE-WORD >= ( n1 n2 -- flag ) 2DUP = ROT ROT > OR END-WORD

\ Fetch a value at an address, add a value to it, and store it to the same
\ address
DEFINE-WORD +! ( n addr -- ) DUP @ ROT + SWAP ! END-WORD

\ Duplicate a value on the top of the stack if it is non-zero
DEFINE-WORD ?DUP ( 0 | x -- 0 | x x ) DUP 0 LIT <> +IF DUP +THEN END-WORD

\ Store a cell-sized value at the HERE pointer and advance the HERE pointer by
\ one cell
DEFINE-WORD , ( x -- ) HERE ! CELL-SIZE ALLOT END-WORD

\ Store a byte-sized value at the HERE pointer and advance the HERE pointer by
\ one byte
DEFINE-WORD C, ( c -- ) HERE C! 1 LIT ALLOT END-WORD

\ Store a 16-bit value at the HERE pointer and advance the HERE pointer by two
\ bytes
DEFINE-WORD H, ( c -- ) HERE H! 2 LIT ALLOT END-WORD

\ Store a 32-bit value at the HERE pointer and advance the HERE pointer by four
\ bytes
DEFINE-WORD W, ( c -- ) HERE W! 4 LIT ALLOT END-WORD

\ Compile a token at the HERE pointer and advance the HERE pointer by the token
\ size
DEFINE-WORD COMPILE, ( token -- )
  NOT-VM TARGET-TOKEN @ TOKEN-8-16 = VM LIT +IF
    DUP $80 LIT U< +IF
      C,
    +ELSE
      DUP $7F LIT AND $80 LIT OR C, 7 LIT RSHIFT 1 LIT - $FF LIT AND C,
    +THEN
  +ELSE
    NOT-VM TARGET-TOKEN @ TOKEN-16 = VM LIT +IF
      $FFFF LIT AND H,
    +ELSE
      NOT-VM TARGET-TOKEN @ TOKEN-16-32 = VM LIT +IF
        DUP $8000 LIT U< +IF
          H,
        +ELSE
          DUP $7FFF LIT AND $8000 LIT OR H, 15 LIT RSHIFT 1 LIT -
	  $FFFF LIT AND H,
	+THEN
      +ELSE ( TARGET-TOKEN @ TOKEN-32 NOT-VM = VM )
        $FFFFFFFF LIT AND W,
      +THEN
    +THEN
  +THEN
END-WORD

\ Standard input file descriptor constant
DEFINE-WORD STDIN ( -- fd ) 0 LIT END-WORD

\ Standard output file descriptor constant
DEFINE-WORD STDOUT ( -- fd ) 1 LIT END-WORD

\ Standard error file descriptor constant
DEFINE-WORD STDERR ( -- fd ) 2 LIT END-WORD

\ TYPE hook
DEFINE-WORD-CREATED 'TYPE
0 SET-CELL-DATA

\ KEY? hook
DEFINE-WORD-CREATED 'KEY?
0 SET-CELL-DATA

\ KEY hook
DEFINE-WORD-CREATED 'KEY
0 SET-CELL-DATA

\ ACCEPT hook
DEFINE-WORD-CREATED 'ACCEPT
0 SET-CELL-DATA

\ BYE hook
DEFINE-WORD-CREATED 'BYE
0 SET-CELL-DATA

\ TYPE wrapper
DEFINE-WORD TYPE ( c-addr bytes -- ) 'TYPE @ EXECUTE END-WORD

\ KEY wrapper
DEFINE-WORD KEY ( -- c ) 'KEY @ EXECUTE END-WORD

\ ACCEPT wrapper
DEFINE-WORD ACCEPT ( c-addr bytes1 -- bytes2 ) 'ACCEPT @ EXECUTE END-WORD

\ BYE wrapper
DEFINE-wORD BYE ( -- ) 'BYE @ EXECUTE END-WORD

\ Space constant
DEFINE-WORD BL $20 LIT END-WORD

\ Newline (LF) constant
DEFINE-WORD NEWLINE $0A LIT END-WORD

\ Tab constant
DEFINE-WORD TAB $09 LIT END-WORD

\ Output a single character on standard output
DEFINE-WORD EMIT ( c -- )
  HERE C! HERE 1 LIT ALLOT 1 LIT TYPE -1 LIT ALLOT END-WORD

\ Output a single space on standard output
DEFINE-WORD SPACE ( -- ) BL EMIT END-WORD

\ Output a single newline on standard output
DEFINE-WORD CR ( -- ) NEWLINE EMIT END-WORD

\ Error writing to standard output
DEFINE-WORD X-UNABLE-TO-WRITE-STDOUT ( -- ) BYE END-WORD

\ Error reading from standard input
DEFINE-WORD X-UNABLE-TO-READ-STDIN SPACE ( -- )
  SPACE S" unable to read from standard input" +DATA TYPE CR
END-WORD

\ Standard input is supposed to be blocking
DEFINE-WORD X-IS-SUPPOSED-TO-BLOCK ( -- )
  SPACE S" standard input is supposed to block" +DATA TYPE CR
END-WORD

\ Exception handler pointer
DEFINE-WORD-CREATED HANDLER
0 SET-CELL-DATA

\ Execute an xt, returning either zero if no exception takes place, or an
\ exception value if an exception has taken place
DEFINE-WORD TRY ( xt -- exception | 0 )
  SP@ >R HANDLER @ >R RP@ HANDLER ! EXECUTE R> HANDLER ! R> DROP 0 LIT
END-WORD

\ If the passed-in value is non-zero, raise an exception; note that this value
\ should be an xt, since uncaught exception values are normally executed as an
\ xt
DEFINE-WORD ?RAISE ( xt | 0 -- xt | 0 )
  ?DUP +IF HANDLER @ RP! R> HANDLER ! R> SWAP >R SP! DROP R> +THEN
END-WORD

\ IF conditional
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY IF ( f -- ) ( Compile-time: -- fore-ref )
  &0BRANCH COMPILE, HERE 0 LIT ,
END-WORD

\ ELSE conditional
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY ELSE ( -- )
  ( Compile-time: fore-ref -- fore-ref )
  &BRANCH COMPILE, HERE 0 LIT , HERE ROT !
END-WORD

\ THEN conditional
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY THEN ( -- ) ( Compile-time: fore-ref -- )
  HERE SWAP !
END-WORD

\ Start of BEGIN/AGAIN, BEGIN/UNTIL, and BEGIN/WHILE/REPEAT loops
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY BEGIN ( -- ) ( Compile-time: -- back-ref )
  HERE
END-WORD

\ Jump back unconditionally to the matching BEGIN
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY AGAIN ( -- ) ( Compile-time: back-ref -- )
  &BRANCH COMPILE, ,
END-WORD

\ Jump back to the matching BEGIN until a value taken off the top of the stack
\ is non-zero
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY UNTIL ( -- ) ( Compile-time: back-ref -- )
  &0BRANCH COMPILE, ,
END-WORD

\ Jump forward to the matching REPEAT if the value taken off the top of the
\ stack is zero
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY WHILE ( -- ) ( Compile_time: -- fore-ref )
  &0BRANCH COMPILE, HERE 0 LIT ,
END-WORD

\ Jump back to the matching BEGIN, and be jumped after by the matching WHILE
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY REPEAT ( -- )
  ( Compile-time: back-ref fore-ref -- )
  SWAP &BRANCH COMPILE, , HERE SWAP !
END-WORD

\ Unimplemented service exception
DEFINE-WORD X-UNIMPLEMENTED-SYS ( -- )
  SPACE S" unimplemented service" +DATA TYPE CR
END-WORD

\ New name for the original SYS opcode because VM assembly words have no
\ "hidden" flag
DEFINE-WORD OLD-SYS ( sys -- ) SYS END-WORD

\ A wrapper for the SYS opcode to handle unimplemented services, raising an
\ exception if they occur
DEFINE-WORD SYS ( sys -- )
  OLD-SYS 0 LIT = +IF &X-UNIMPLEMENTED-SYS ?RAISE +THEN
END-WORD

\ Look up a service by name and return either the service ID, if the service
\ exists, or zero, if it does not exist
DEFINE-WORD SYS-LOOKUP ( addr bytes -- service|0 ) 1 LIT SYS END-WORD

\ Read service ID
DEFINE-WORD-CREATED SYS-READ
0 SET-CELL-DATA

\ Write service ID
DEFINE-WORD-CREATED SYS-WRITE
0 SET-CELL-DATA

\ Get nonblocking service ID
DEFINE-WORD-CREATED SYS-GET-NONBLOCKING
0 SET-CELL-DATA

\ Set nonblocking service ID
DEFINE-WORD-CREATED SYS-SET-NONBLOCKING
0 SET-CELL-DATA

\ Bye service ID
DEFINE-WORD-CREATED SYS-BYE
0 SET-CELL-DATA

\ Get trace service ID
DEFINE-WORD-CREATED SYS-GET-TRACE
0 SET-CELL-DATA

\ Set trace service ID
DEFINE-WORD-CREATED SYS-SET-TRACE
0 SET-CELL-DATA

\ Set name table service ID
DEFINE-WORD-CREATED SYS-SET-NAME-TABLE
0 SET-CELL-DATA

\ Look up a number of services
DEFINE-WORD LOOKUP-SYS ( -- )
  S" READ" +DATA SYS-LOOKUP SYS-READ !
  S" WRITE" +DATA SYS-LOOKUP SYS-WRITE !
  S" GET-NONBLOCKING" +DATA SYS-LOOKUP SYS-GET-NONBLOCKING !
  S" SET-NONBLOCKING" +DATA SYS-LOOKUP SYS-SET-NONBLOCKING !
  S" BYE" +DATA SYS-LOOKUP SYS-BYE !
  S" GET-TRACE" +DATA SYS-LOOKUP SYS-GET-TRACE !
  S" SET-TRACE" +DATA SYS-LOOKUP SYS-SET-TRACE !
  S" SET-NAME-TABLE" +DATA SYS-LOOKUP SYS-SET-NAME-TABLE !
END-WORD

\ Read a file descriptor (a return value of -1 means success, a return value of
\ 0 means error, and a return value of 1 means that accessing would block were
\ it not for non-blocking).
DEFINE-WORD READ ( c-addr bytes fd -- bytes-read -1|0|1 )
  SYS-READ @ SYS
END-WORD

\ Write a file descriptor (a return value of -1 means success, a return value of
\ 0 means error, and a return value of 1 means that accessing would block were
\ it not for non-blocking).
DEFINE-WORD WRITE ( c-addr bytes fd -- bytes-written -1|0|1 )
  SYS-WRITE @ SYS
END-WORD

\ Get whether a file descriptor is non-blocking (a return value of -1 means
\ success and a return value 0f 0 means error).
DEFINE-WORD GET-NONBLOCKING ( fd -- non-blocking -1|0 )
  SYS-GET-NONBLOCKING @ SYS
END-WORD

\ Get whether a file descriptor is blocking (a return value of -1 means success
\ and a return value of 0 means error).
DEFINE-WORD SET-NONBLOCKING ( non-blocking fd -- -1|0 )
  SYS-SET-NONBLOCKING @ SYS
END-WORD

\ Get the trace flag
DEFINE-WORD GET-TRACE ( -- trace-flag ) SYS-GET-TRACE @ SYS END-WORD

\ Set the trace flag
DEFINE-WORD SET-TRACE ( trace-flag -- ) SYS-SET-TRACE @ SYS END-WORD

\ Set the name table
DEFINE-WORD SET-NAME-TABLE ( addr -- ) SYS-SET-NAME-TABLE @ SYS END-WORD

\ Whether to treat IO as single-tasking.
DEFINE-WORD-CREATED SINGLE-TASK-IO
0 SET-CELL-DATA

\ The base for numeric formatting
DEFINE-WORD-CREATED BASE
10 SET-CELL-DATA

\ The size of the buffer for numeric formatting
NOT-VM 65 CONSTANT MAX-FORMAT-DIGIT-COUNT VM

\ A buffer for numeric formatting
DEFINE-WORD-CREATED FORMAT-DIGIT-BUFFER
MAX-FORMAT-DIGIT-COUNT 0 SET-FILL-DATA

\ Convert a pointer into the numeric fomatting buffer into a pointer and a
\ length in bytes
DEFINE-WORD COMPLETE-FORMAT-DIGIT-BUFFER ( c-addr -- c-addr u )
  FORMAT-DIGIT-BUFFER MAX-FORMAT-DIGIT-COUNT LIT + OVER -
END-WORD

\ Add a character to the numeric formatting buffer and return an updated
\ pointer
DEFINE-WORD ADD-CHAR ( c c-addr -- c-addr ) 1 LIT - TUCK C! END-WORD

\ The inner loop for formatting decimal numbers
DEFINE-WORD (FORMAT-DECIMAL) ( n -- c-addr )
  FORMAT-DIGIT-BUFFER MAX-FORMAT-DIGIT-COUNT LIT + +BEGIN
    OVER 0 LIT U>
  +WHILE
    OVER 10 LIT UMOD CHAR 0 LIT + SWAP ADD-CHAR SWAP 10 LIT U/ SWAP
  +REPEAT
  NIP
END-WORD

\ Handle the outer parts of formatting decimal numbers
DEFINE-WORD FORMAT-DECIMAL ( n -- c-addr u )
  DUP 0 LIT < +IF
    NEGATE (FORMAT-DECIMAL) CHAR - LIT SWAP ADD-CHAR
    COMPLETE-FORMAT-DIGIT-BUFFER
  +ELSE
    (FORMAT-DECIMAL) COMPLETE-FORMAT-DIGIT-BUFFER
  +THEN
END-WORD

\ Inner portion of formatting unsigned and non-decimal numbers
DEFiNE-WORD FORMAT-UNSIGNED ( n -- c-addr u )
  FORMAT-DIGIT-BUFFER MAX-FORMAT-DIGIT-COUNT LIT + +BEGIN
    OVER 0 LIT U>
  +WHILE
    OVER BASE @ UMOD DUP 10 LIT U< +IF
      CHAR 0 LIT +
    +ELSE
      CHAR A LIT + 10 LIT -
    +THEN
    SWAP ADD-CHAR SWAP BASE @ U/ SWAP
  +REPEAT
  NIP COMPLETE-FORMAT-DIGIT-BUFFER
END-WORD

\ Format signed numbers and numbers of any base (note that non-decimal numbers
\ are treated as unsigned)
DEFINE-WORD FORMAT-NUMBER ( n -- c-addr u )
  DUP 0 LIT = +IF
    DROP CHAR 0 LIT FORMAT-DIGIT-BUFFER C! FORMAT-DIGIT-BUFFER 1 LIT
  +ELSE BASE @ 10 LIT = +IF
    FORMAT-DECIMAL
  +ELSE BASE @ DUP 2 LIT >= SWAP 36 LIT <= AND +IF
    FORMAT-UNSIGNED
  +ELSE
    DROP FORMAT-DIGIT-BUFFER 0 LIT
  +THEN +THEN +THEN
END-WORD

\ Format unsigned numbers
DEFINE-WORD FORMAT-UNSIGNED ( n -- c-addr u )
  DUP 0 LIT = +IF
    DROP CHAR 0 LIT FORMAT-DIGIT-BUFFER C! FORMAT-DIGIT-BUFFER 1 LIT
  +ELSE BASE @ DUP 2 LIT >= SWAP 36 LIT <= AND +IF
    FORMAT-UNSIGNED
  +ELSE
    DROP FORMAT-DIGIT-BUFFER 0 LIT
  +THEN +THEN
END-WORD

\ Output a signed number on standard output with no following space
DEFINE-WORD (.) ( n -- ) FORMAT-NUMBER TYPE END-WORD

\ Output an unsigned number on standard output with no following space
DEFINE-WORD (U.) ( u -- ) FORMAT-UNSIGNED TYPE END-WORD

\ Output a signed number on standard output with a following space
DEFINE-WORD . ( n -- ) (.) SPACE END-WORD

\ Output an unsigned number on standard output with a following space
DEFINE-WORD U. ( u -- ) (U.) SPACE END-WORD

\ Base address of the data stack (note the data stack grows downward)
DEFINE-WORD-CREATED SBASE
0 SET-CELL-DATA

\ Base address of the return stack (note the return stack grows downward)
DEFINE-WORD-CREATED RBASE
0 SET-CELL-DATA

\ Get the depth of the data stack
DEFINE-WORD DEPTH ( -- u ) SBASE @ SP@ - CELL-SIZE / 1 LIT - END-WORD

\ Output the contents of the data stack, from deepest to topmost, on standard
\ output
DEFINE-WORD .S ( -- )
  DEPTH 0 LIT >= +IF
    CHAR [ LIT EMIT SPACE DEPTH +BEGIN DUP 0 LIT U> +WHILE
      DUP PICK . 1 LIT -
    +REPEAT
    DROP
    CHAR ] LIT EMIT
  +ELSE
    CHAR [ LIT EMIT CHAR - LIT EMIT CHAR - LIT EMIT CHAR ] LIT EMIT
  +THEN
END-WORD

\ The size of the saved exception string
NOT-VM 256 CONSTANT MAX-SAVED-EXCEPTION-LEN VM

\ The saved exception string buffer
DEFINE-WORD-CREATED SAVED-EXCEPTION-BUFFER
MAX-SAVED-EXCEPTION-LEN 0 SET-FILL-DATA

\ The saved exception string length
DEFINE-WORD-CREATED SAVED-EXCEPTION-LEN
0 SET-CELL-DATA

\ Copy data from lowest to highest addresses
DEFINE-WORD CMOVE ( c-addr1 c-addr2 bytes -- )
  +BEGIN DUP 0 LIT U> +WHILE
    2 LIT PICK C@ 2 LIT PICK C! 1 LIT - ROT 1 LIT + ROT 1 LIT + ROT
  +REPEAT
  2DROP DROP
END-WORD

\ Copy data from highest to lowest addresses
DEFINE-WORD CMOVE> ( c-addr1 c-addr2 bytes -- )
  +BEGIN DUP 0 LIT U> +WHILE
    1 LIT - 2 LIT PICK OVER + C@ 2 LIT PICK 2 LIT PICK + C!
  +REPEAT
  2DROP DROP
END-WORD

\ Copy data in such a manner that overlapping blocks of memory can be safely
\ copied to one another
DEFINE-WORD MOVE ( c-addr1 c-addr2 bytes -- )
  2 LIT PICK 2 LIT PICK < +IF CMOVE> +ELSE CMOVE +THEN
END-WORD

\ Save a string for an exception
DEFINE-WORD SAVE-EXCEPTION ( c-addr bytes -- )
  DUP MAX-SAVED-EXCEPTION-LEN LIT U> +IF DROP MAX-SAVED-EXCEPTION-LEN LIT +THEN
  DUP SAVED-EXCEPTION-LEN ! SAVED-EXCEPTION-BUFFER SWAP CMOVE
END-WORD

\ Get a string saved for an exception
DEFINE-WORD SAVED-EXCEPTION ( -- c-addr bytes )
  SAVED-EXCEPTION-BUFFER SAVED-EXCEPTION-LEN @
END-WORD

\ Advance a pointer and size for a buffer.
DEFINE-WORD ADVANCE-BUFFER ( c-addr bytes bytes-to-advance -- c-addr bytes )
  ROT OVER + ROT ROT -
END-WORD

\ Single-tasking output to standard output implementation
DEFINE-WORD (TYPE) ( c-addr bytes -- )
  STDOUT GET-NONBLOCKING 0 LIT = +IF &X-UNABLE-TO-WRITE-STDOUT ?RAISE +THEN
  +FALSE STDOUT SET-NONBLOCKING 0 LIT = +IF
    &X-UNABLE-TO-WRITE-STDOUT ?RAISE
  +THEN
  ROT ROT +BEGIN DUP 0 LIT > +WHILE
    2DUP STDOUT WRITE 0 LIT = +IF &X-UNABLE-TO-WRITE-STDOUT ?RAISE +THEN
    ADVANCE-BUFFER
  +REPEAT
  2DROP STDOUT SET-NONBLOCKING 0 LIT = +IF
    &X-UNABLE-TO-WRITE-STDOUT ?RAISE
  +THEN
END-WORD

\ Currently read character
DEFINE-WORD-CREATED READ-KEY
0 SET-CELL-DATA

\ Whether a key has been read
DEFINE-WORD-CREATED READ-KEY?
0 SET-CELL-DATA

\ Single-tasking implementation of testing whether a key is read.
DEFINE-WORD (KEY?) ( -- flag )
  READ-KEY? @ +IF
    +TRUE
  +ELSE
    STDIN GET-NONBLOCKING 0 LIT = +IF &X-UNABLE-TO-READ-STDIN ?RAISE +THEN
    +TRUE STDIN SET-NONBLOCKING 0 LIT = +IF &X-UNABLE-TO-READ-STDIN ?RAISE +THEN
    HERE 1 LIT 1 LIT ALLOT STDIN READ DUP 0 LIT = +IF
      &X-UNABLE-TO-READ-STDIN ?RAISE
    +ELSE -1 LIT = +IF
      -1 LIT ALLOT 1 LIT = +IF
        HERE C@ READ-KEY ! +TRUE
      ELSE
        &X-UNABLE-TO-READ-STDIN ?RAISE THEN
      THEN
    +ELSE
      -1 LIT ALLOT +FALSE
    +THEN +THEN
    SWAP STDIN SET-NONBLOCKING 0 LIT = +IF &X-UNABLE-TO-READ-STDIN ?RAISE +THEN
  +THEN
END-WORD

\ Single-tasking implementation of reading a keypress from standard input.
DEFINE-WORD (KEY) ( -- c )
  READ-KEY? @ +IF
    READ-KEY @ +FALSE READ-KEY? !
  +ELSE
    STDIN GET-NONBLOCKING 0 LIT = +IF &X-UNABLE-TO-READ-STDIN ?RAISE +THEN
    +FALSE STDIN SET-NONBLOCKING 0 LIT = +IF
      &X-UNABLE-TO-READ-STDIN ?RAISE
    +THEN
    HERE 1 LIT 1 LIT ALLOT STDIN READ DUP 0 LIT = +IF
      &X-UNABLE-TO-READ-STDIN ?RAISE
    +ELSE -1 LIT = +IF
     -1 LIT ALLOT 1 LIT = +IF HERE C@ +ELSE &X-UNABLE-TO-READ-STDIN ?RAISE +THEN
    +ELSE
      &X-IS-SUPPOSED-TO-BLOCK ?RAISE
    +THEN +THEN
    SWAP STDIN SET-NONBLOCKING 0 LIT = +IF
      &X-UNABLE-TO-READ-STDIN ?RAISE
    +THEN
  +THEN
END-WORD

\ The size of the terminal input buffer
NOT-VM 1024 CONSTANT TIB-SIZE VM

\ The terminal input buffer size constant
NON-DEFINE-WORD TIB-SIZE ( -- bytes ) TIB-SIZE LIT END-WORD

\ The terminal input buffer
DEFINE-WORD-CREATED TIB
TIB-SIZE 0 SET-FILL-DATA

\ The number of characters currently in the terminal input buffer
DEFINE-WORD-CREATED TIB#
0 SET-CELL-DATA

\ Read terminal input into a buffer
DEFINE-WORD (ACCEPT) ( c-addr bytes -- bytes-read )
  0 LIT TIB# !
  +BEGIN
    TIB-SIZE LIT TIB# @ > +IF
      KEY DUP TIB TIB# @ + C! NEWLINE = +IF
        TRUE
      +ELSE
        TIB# @ 1 LIT + TIB# ! FALSE
      +THEN
    +ELSE
      TRUE
    +THEN
  +UNTIL
  DUP TIB# @ > +IF DROP TIB# @ +THEN SWAP TIB SWAP 2 LIT PICK CMOVE
END-WORD

\ The implementation of the BYE word, invoking the BYE service
DEFINE-WORD (BYE) ( -- ) SYS-BYE @ SYS END-WORD

\ The PAUSE hook
DEFINE-WORD-CREATED 'PAUSE
0 SET-CELL-DATA

\ The PAUSE wrapper
DEFINE-WORD PAUSE ( -- ) 'PAUSE @ ?DUP +IF EXECUTE +THEN END-WORD

\ The default implementation of PAUSE
DEFINE-WORD (PAUSE) ( -- ) END-WORD

\ Set a number of hooks
DEFINE-WORD SET-HOOKS ( -- )
  &(TYPE) 'TYPE !
  &(KEY?) 'KEY? !
  &(KEY) 'KEY !
  &(ACCEPT) 'ACCEPT !
  &(BYE) 'BYE !
  &(PAUSE) 'PAUSE !
END-WORD

\ The Forth wordlist constant
DEFINE-WORD FORTH-WORDLIST ( -- wid ) 0 LIT END-WORD

\ The maximum number of wordlists
NOT-VM 256 CONSTANT MAX-WORDLIST-COUNT VM

\ The array of wordlists' most recent entries
DEFINE-WORD-CREATED WORDLIST-ARRAY
NOT-VM TARGET-CELL-SIZE MAX-WORDLIST-COUNT * VM 0 SET-FILL-DATA

\ The number of existing wordlist
DEFINE-WORD-CREATED WORDLIST-COUNT
1 SET-CELL-DATA

\ The maximum number of wordlist reached exception
DEFINE-WORD X-MAX-WORDLISTS ( -- )
  SPACE S" max wordlists reached" +DATA TYPE CR
END-WORD

\ The out of range wordlist exception
DEFINE-WORD X-OUT-OF-RANGE-WORDLIST ( -- )
  SPACE S" out of range wordlist id" +DATA TYPE CR
END-WORD

\ Return a new wordlist
DEFINE-WORD WORDLIST ( -- wid )
  WORDLIST-COUNT @ MAX-WORDLIST-COUNT LIT < +IF
    0 LIT WORDLIST-ARRAY WORDLIST-COUNT @ CELLS + !
    WORDLIST-COUNT @ DUP 1 LIT + WORDLIST-COUNT !
  +ELSE
    &X-MAX-WORDLISTS ?RAISE
  +THEN
END-WORD

\ Get the most recent entry (an xt) in a wordlist, or zero if the wordlist is
\ empty
DEFINE-WORD WORDLIST>FIRST ( wid -- xt )
  DUP WORDLIST-COUNT @ < +IF
    CELLS WORDLIST-ARRAY + @
  +ELSE
    &X-OUT-OF-RANGE-WORDLIST ?RAISE
  +THEN
END-WORD

\ Set the most recent entry (an xt) in a wordlist, zero if the wordlist is
\ to be empty
DEFINE-WORD FIRST>WORDLIST ( xt wid -- )
  DUP WORDLIST-COUNT @ < +IF
    CELLS WORDLIST-ARRAY + !
  +ELSE
    &X-OUT-OF-RANGE-WORDLIST ?RAISE
  +THEN
END-WORD

\ The wordlist for which words are currently compiled for
DEFINE-WORD-CREATED CURRENT-WORDLIST
0 SET-CELL-DATA

\ Set the wordlist for which words are currently compiled for
DEFINE-WORD SET-CURRENT ( wid -- ) CURRENT-WORDLIST ! END-WORD

\ Get the wordlist for which words are currently compiled for
DEFINE-WORD GET-CURRENT ( -- wid ) CURRENT-WORDLIST @ END-WORD

\ Convert an ASCII character to be upper case
DEFINE-WORD UPCASE-CHAR ( c -- c )
  DUP CHAR a LIT >= OVER CHAR z LIT <= AND +IF CHAR a LIT - CHAR A LIT + +THEN
END-WORD

\ Get whether two equal-length arrays of ASCII characters are equal when
\ differing cases are ignored
DEFINE-WORD EQUAL-CASE-CHARS? ( c-addr1 c-addr2 bytes -- matches )
  +BEGIN DUP 0 LIT > +WHILE
    2 LIT PICK C@ UPCASE-CHAR 2 LIT PICK C@ UPCASE-CHAR <> +IF
      2DROP DROP +FALSE EXIT
    +THEN
    1 LIT - ROT 1 LIT + ROT 1 LIT + ROT
  +REPEAT
  2DROP DROP +TRUE
END-WORD

\ Get whether two equal-length arrays of ASCII characters are equal
DEFINE-WORD EQUAL-CHARS? ( c-addr1 c-addr2 bytes -- matches )
  +BEGIN DUP 0 LIT > +WHILE
    2 LIT PICK C@ 2 LIT PICK C@ <> +IF
      2DROP DROP +FALSE EXIT
    +THEN
    1 LIT - ROT 1 LIT + ROT 1 LIT + ROT
  +REPEAT
  2DROP DROP +TRUE
END-WORD

\ Get whether two names composed of ASCII characters are equal, ignoring case
DEFINE-WORD EQUAL-NAME? ( c-addr1 bytes1 c-addr2 bytes2 -- matches )
  DUP 3 LIT PICK = +IF
    DROP SWAP EQUAL-CASE-CHARS?
  +ELSE
    2DROP 2DROP +FALSE
  +THEN
END-WORD

\ Get whether two strings are equal, taking case into account
DEFINE-WORD EQUAL-STRINGS? ( c-addr1 bytes1 c-addr2 bytes2 -- matches )
  DUP 3 LIT PICK = +IF
    DROP SWAP EQUAL-CHARS?
  +ELSE
    2DROP 2DROP +FALSE
  +THEN
END-WORD

\ Get the flags for a word (an xt)
DEFINE-WORD WORD>FLAGS ( xt -- flags )
  2 LIT CELLS * INFO-TABLE + @
END-WORD

\ Set the flags for a word (an xt)
DEFINE-WORD FLAGS>WORD ( flags xt -- )
  2 LIT CELLS * INFO-TABLE + !
END-WORD

\ Get the next word for a word (an xt), or zero if there is no next word
DEFINE-WORD WORD>NEXT ( xt1 -- xt2 )
  2 LIT CELLS * INFO-TABLE + CELL+ @
END-WORD

\ Set the next word for a word (an xt), or zero if there is no next word
DEFINE-WORD NEXT>WORD ( xt1 xt2 -- )
  2 LIT CELLS * INFO-TABLE + CELL+ !
END-WORD

\ Get the name for a word (an xt)
DEFINE-WORD WORD>NAME ( xt -- addr bytes )
  2 LIT CELLS * NAME-TABLE + DUP @ SWAP CELL+ @
END-WORD

\ Set the name for a word (an xt)
DEFINE-WORD NAME>WORD ( addr bytes xt -- )
  2 LIT CELLS * NAME-TABLE + ROT OVER ! CELL+ !
END-WORD

\ Search a wordlist for a word (an xt) by name, returning a non-zero value and
\ an xt if the word is found in the wordlist, else returning zero and zero
DEFINE-WORD SEARCH-WORDLIST ( c-addr bytes wid -- xt found )
  WORDLIST>FIRST +BEGIN DUP 0 LIT <> +WHILE
    DUP WORD>FLAGS HIDDEN-FLAG LIT AND 0 LIT = +IF
      2 LIT PICK 2 LIT PICK 2 LIT PICK WORD>NAME EQUAL-NAME? +IF
        ROT ROT 2DROP +TRUE EXIT
      +THEN
    +THEN
    WORD>NEXT
  +REPEAT
  2DROP DROP 0 LIT +FALSE
END-WORD

\ The maximum wordlist order size
NOT-VM 128 CONSTANT MAX-WORDLIST-ORDER-COUNT VM

\ The wordlist order array, ordered from first to last wordlist
DEFINE-WORD-CREATED WORDLIST-ORDER-ARRAY
NOT-VM TARGET-CELL-SIZE MAX-WORDLIST-ORDER-COUNT * 0 SET-FILL-DATA VM

\ The number of entries in the wordlist order
DEFINE-WORD-CREATED WORDLIST-ORDER-COUNT
1 SET-CELL-DATA

\ The maximum wordlist order size reached exception
DEFINE-WORD X-MAX-WORDLIST-ORDER-COUNT ( -- )
  SPACE S" max wordlist order count reached" +DATA TYPE CR
END-WORD

\ Set the wordlist order with a number of entries in the wordlist combined with
\ that number of wordlist IDs popped off the data stack, in order from first
\ to last
DEFINE-WORD SET-ORDER ( widn ... wid1 count -- )
  DUP MAX-WORDLIST-ORDER-COUNT LIT <= +IF
    DUP WORDLIST-ORDER-COUNT !
    0 LIT +BEGIN 2DUP > +WHILE
      ROT OVER CELLS WORDLIST-ORDER-ARRAY + ! 1 LIT +
    +REPEAT
    2DROP
  +ELSE
    &X-MAX-WORDLIST-ORDER-COUNT ?RAISE
  +THEN
END-WORD

\ Get the wordlist order with the entries of the wordlist order pushed onto
\ the data stack, in order from last to first, followed by the number of entries
\ in the wordlist order
DEFINE-WORD GET-ORDER ( -- widn ... wid1 count )
  WORDLIST-ORDER-COUNT @ +BEGIN DUP 0 LIT > +WHILE
    1 LIT - DUP CELLS WORDLIST-ORDER-ARRAY + @ SWAP
  +REPEAT
  DROP WORDLIST-ORDER-COUNT @
END-WORD

\ Search all the wordlists, in order from first to last, for a word by name,
\ and if that word is found, it is pushed onto the stack as an xt followed by
\ a non-zero value, else zero followed by zero is pushed onto the data stack
DEFINE-WORD SEARCH-WORDLISTS ( c-addr bytes -- xt found )
  0 LIT +BEGIN DUP WORDLIST-ORDER-COUNT @ < +WHILE
    DUP CELL-SIZE * WORDLIST-ORDER-ARRAY + @ 3 LIT PICK 3 LIT PICK ROT
    SEARCH-WORDLIST +IF
      SWAP DROP SWAP DROP SWAP DROP +TRUE EXIT
    +ELSE
      DROP 1 LIT +
    +THEN
  +REPEAT
  2DROP DROP 0 LIT +FALSE
END-WORD

\ A pointer to the interpreter input bufer
DEFINE-WORD-CREATED INPUT
0 SET-CELL-DATA

\ The number of bytes in the interpreter input buffer
DEFINE-WORD-CREATED INPUT#
0 SET-CELL-DATA

\ The current offset in bytes in the interpreter input buffer
DEFINE-WORD-CREATED >IN
0 SET-CELL-DATA

\ The compile-only word exception
DEFINE-WORD X-COMPILE-ONLY-ERROR ( -- )
  SPACE SAVED-EXCEPTION TYPE S" : compile-only word" +DATA TYPE CR
END-WORD

\ The parser error (unrecognized data in interpreter input buffer) exception
DEFINE-WORD X-PARSE-ERROR ( -- )
  SPACE SAVED-EXCEPTION TYPE S" : parse error" +DATA TYPE CR
END-WORD

\ The no data available to parse in the interpreter input buffer exception
DEFINE-WORD X-NO-PARSE-FOUND ( -- )
  SPACE S" input expected" +DATA TYPE CR
END-WORD

\ The parsed name found does not correspond to any word exception
DEFINE-WORD X-NO-WORD-FOUND ( -- )
  SPACE SAVED-EXCEPTION TYPE S" : word not found" +DATA TYPE CR
END-WORD

\ The no LATESTXT value exists exception
DEFINE-WORD X-NO-LATESTXT ( -- )
  SPACE S" no latestxt exists" +DATA TYPE CR
END-WORD

\ Get whether any characters are left in the interpreter input buffer
DEFINE-WORD INPUT-LEFT? ( -- f ) >IN @ INPUT# @ < END-WORD

\ Get whether the current character in the interpreter input buffer is not
\ whitespace
DEFINE-WORD INPUT-NOT-WS? ( -- f )
  INPUT @ >IN @ + C@ DUP BL <> OVER NEWLINE <> AND SWAP TAB <> AND  
END-WORD

\ Advance the current character in the interpreter input buffer by one byte
DEFINE-WORD ADVANCE->IN ( -- ) >IN @ 1 LIT + >IN ! END-WORD

\ Advance the current character in the interpreter input buffer to the next
\ non-whitespace character, and return a non-zero value if a non-whitespace
\ character is found else return zero
DEFINE-WORD ADVANCE-TO-NAME-START ( -- found )
  +BEGIN
    INPUT-LEFT? +IF
      INPUT-NOT-WS? +IF +TRUE +TRUE +ELSE ADVANCE->IN +FALSE +THEN
    +ELSE
      +FALSE +TRUE
    +THEN
  +UNTIL
END-WORD

\ Advance the current character in the interpreter input buffer to the next
\ whitespace character or the end of the interpreter input buffer, whichever
\ comes first
DEFINE-WORD ADVANCE-TO-NAME-END ( -- )
  +BEGIN
    INPUT-LEFT? +IF
      INPUT-NOT-WS? +IF ADVANCE->IN +FALSE +ELSE +TRUE +THEN
    +ELSE
      +TRUE
    +THEN
  +UNTIL
END-WORD

\ Parse a name, and return the string found in the interpreter input buffer for
\ it if it is found, else return zero followed by zero
DEFINE-WORD PARSE-NAME ( -- c-addr bytes )
  ADVANCE-TO-NAME-START +IF
    >IN @ ADVANCE-TO-NAME-END INPUT @ OVER + >IN @ ROT -
  +ELSE
    0 LIT 0 LIT
  +THEN
END-WORD

\ Get whether the current character in the interpreter input buffer is a close
\ paren
DEFINE-WORD INPUT-CLOSE-PAREN? ( -- f ) INPUT @ >IN @ + C@ CHAR ) LIT = END-WORD

\ Parse a paren comment
NON-DEFINE-WORD-IMMEDIATE ( ( -- )
  +BEGIN
    INPUT-LEFT? +IF INPUT-CLOSE-PAREN? ADVANCE->IN +ELSE +TRUE +THEN
  +UNTIL
END-WORD

\ Get whether the current character in the interpreter input buffer is a newline
DEFINE-WORD INPUT-NEWLINE? ( -- f ) INPUT @ >IN @ + C@ NEWLINE = END-WORD

\ Parse a backslash comment
NON-DEFINE-WORD-IMMEDIATE \ ( -- )
  +BEGIN
    INPUT-LEFT? +IF INPUT-NEWLINE? ADVANCE->IN +ELSE +TRUE +THEN
  +UNTIL
END-WORD

\ Get whether the current character in the interpreter input buffer is a quote
DEFINE-WORD INPUT-QUOTE? ( -- f ) INPUT @ >IN @ + C@ CHAR " LIT = END-WORD

\ Parse a string closed by a quote and return it on the data stack
DEFINE-WORD PARSE-STRING ( "ccc<quote>" -- c-addr bytes )
  ADVANCE-TO-NAME-START +IF
    INPUT @ >IN @ + 0 LIT +BEGIN
      INPUT-LEFT? +IF
        INPUT-QUOTE? +IF +TRUE +ELSE 1 LIT + +FALSE +THEN ADVANCE->IN
      +ELSE
        +TRUE
      +THEN
    +UNTIL
  +ELSE
    &X-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

\ Get the minimum of two signed values
DEFINE-WORD MIN ( n1 n2 -- n3 ) 2DUP > +IF SWAP +THEN DROP END-WORD

\ Get the maximum of two signed values
DEFINE-WORD MAX ( n1 n2 -- n3 ) 2DUP < +IF SWAP +THEN DROP END-WORD

\ Zero in interpretation mode, non-zero in compilation mode
DEFINE-WORD-CREATED STATE
0 SET-CELL-DATA

\ The size of a buffer for interpreter S"
NOT-VM 1024 CONSTANT STRING-BUFFER-SIZE VM

\ The buffer for interpreter S" usage -  note that only one interpreter S"
\ string may be used at a time
DEFINE-WORD-CREATED STRING-BUFFER
STRING-BUFFER-SIZE 0 SET-FILL-DATA

\ In interpretation mode, save a string into a buffer and push the address and
\ length of bytes of the string onto the stack; in compilation mode, compile
\ an immediately parsed string such that a pointer to the string combined with
\ the length of the string in bytes is pushed onto the stack when the code
\ being compiled is executed
NON-DEFINE-WORD-IMMEDIATE S" ( "ccc<quote>" -- )
  ( Runtime: -- c-addr bytes )
  PARSE-STRING STATE @ +IF
    &(DATA) COMPILE, DUP , SWAP HERE 2 LIT PICK CMOVE
    DUP ALLOT &(LIT) COMPILE, ,
  +ELSE
    SWAP STRING-BUFFER ROT STRING-BUFFER-SIZE LIT MIN >R R@ CMOVE
    STRING-BUFFER R>
  +THEN
END-WORD

\ Compile an immediately parsed string such that the string is outputted to
\ standard output when the code being compiled is executed
NON-DEFINE-WORD-IMMEDIATE-COMPILE-ONLY ." ( Compile-time "ccc<quote>" -- )
  ( Runtime: -- c-addr bytes )
  PARSE-STRING
  &(DATA) COMPILE, DUP , SWAP HERE 2 LIT PICK CMOVE DUP ALLOT &(LIT) COMPILE, ,
  &TYPE COMPILE,
END-WORD

\ Parse a string closed by a close paren and return it on the data stack
DEFINE-WORD PARSE-PAREN-STRING ( "ccc<quote>" -- c-addr bytes )
  ADVANCE-TO-NAME-START +IF
    INPUT @ >IN @ + 0 LIT +BEGIN
      INPUT-LEFT? +IF
        INPUT-CLOSE-PAREN? +IF +TRUE +ELSE 1 LIT + +FALSE +THEN ADVANCE->IN
      +ELSE
        +TRUE
      +THEN
    +UNTIL
  +ELSE
    &X-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

\ Immediately parse a string closed by a close paren and output it to standard
\ output
NON-DEFINE-WORD-IMMEDIATE .( ( Compile-time "ccc<close-paren>" -- )
  ( Runtime: -- c-addr bytes )
  PARSE-PAREN-STRING TYPE
END-WORD

\ Parse a name and push its first character onto the data stack, discarding the
\ remainder of the name
NON-DEFINE-WORD CHAR ( -- c )
  PARSE-NAME 0 LIT <> +IF
    C@
  +ELSE
    DROP &X-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

\ Immediately parse a name and compile its first character as a literal, so that
\ it will be pushed onto the data stack when the code compiled is executed
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY [CHAR] ( Run-time: -- c )
  ^CHAR TOKEN, &(LIT) COMPILE, ,
END-WORD

\ Parse a name and look it up in the wordlists, pushing its xt onto the data
\ stack if it is found and throwing an exception if it is not
DEFINE-WORD ' ( "name" -- xt )
  PARSE-NAME DUP 0 LIT <> +IF
    2DUP SEARCH-WORDLISTS +IF
      ROT ROT 2DROP
    +ELSE
      DROP SAVE-EXCEPTION &X-NO-WORD-FOUND ?RAISE
    +THEN
  +ELSE
    2DROP &X-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

\ Raise a specified exception if the value passed in along with the xt is zero
DEFINE-WORD ??RAISE ( cond xt|0 -- xt|... )
  SWAP 0 LIT = +IF ?RAISE +ELSE DROP +THEN
END-WORD

\ Immediately parse a name and look it up in the wordlists; if at runtime the
\ value passed in is zero this xt will be used to raise an exception otherwise
\ it is discarded
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY AVERTS ( Compile-time: "name" -- )
  ( Run-time: asserted -- xt|... )
  &(LIT) COMPILE, ' ,  &??RAISE COMPILE,
END-WORD

\ Compile a value on the data stack as a literal, so it is pushed onto the data
\ stack when the code compiled is executed at runtme
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY LITERAL ( Compile-time: x -- )
  ( Run-time: -- x )
  &(LIT) COMPILE, ,
END-WORD

\ Immediately parse a name and look it up in the wordlists, compiling its xt
\ into the code being compiled so it is pushed onto the data stack when it is
\ executed if it is found and throwing an exception if it is not
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY ['] ( Compile-time: "name" -- )
  ( Run-time: -- xt )
  &(LIT) COMPILE, ' ,
END-WORD

\ Parse a name and look it up in the wordlists, compiling code that compiles
\ its xt into the code being compiled if it is found and throwing an exception
\ if it is not
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY COMPILE ( Compile-time: "name" -- )
  &(LIT) COMPILE, ' , &COMPILE, COMPILE,
END-WORD

\ Parse a name and look it up in the wordlists, compiling its xt into the code
\ being compiled if it is found and throwing an exception if it is not
DEFINE-WORD-IMMEDIATE-COMPILE-ONLY [COMPILE] ( Compile-time: "name" -- )
  ' COMPILE,
END-WORD

\ Get whether a word (as an xt) is immediate
DEFINE-WORD IMMEDIATE? ( xt -- flag )
  WORD>FLAGS IMMEDIATE-FLAG LIT AND 0 LIT <>
END-WORD

\ The actual value of LATESTXT
DEFINE-WORD-CREATED LATESTXT-VALUE
0 SET-CELL-DATA

\ Allocate space for a string in user space and copy a string into it, pushing
\ the allocated string's address and length in bytes on the data stack
DEFINE-WORD ALLOCATE-STRING ( c-addr bytes -- c-addr bytes )
  HERE OVER ALLOT ROT 2 LIT PICK 2 LIT PICK SWAP CMOVE SWAP
END-WORD

\ Parse a name and begin compilation of a word with that name, setting the word
\ to have a secondary code pointer to the address in the data stack after
\ where the word's name is copied, setting the word to have a null data
\ pointer, setting the word to be hidden, setting LATESTXT to the word, and
\ adding the word to the current wordlist
NON-DEFINE-WORD : ( -- )
  PARSE-NAME DUP 0 LIT <> +IF
    ALLOCATE-STRING +TRUE STATE ! HERE NEW-COLON DUP 3 LIT ROLL 3 LIT ROLL ROT
\    S" compiling: " +DATA TYPE 2 LIT PICK 2 LIT PICK TYPE CR
    NAME>WORD HIDDEN-FLAG LIT OVER FLAGS>WORD DUP LATESTXT-VALUE !
    GET-CURRENT WORDLIST>FIRST OVER NEXT>WORD
    GET-CURRENT FIRST>WORDLIST
  +ELSE
    2DROP &X-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

\ Begin compilation of an anonymous word, setting LATESTXT to the word, and
\ pushing the xt for the word onto the data stack
DEFINE-WORD :NONAME ( -- xt )
  TRUE STATE ! HERE NEW-COLON DUP LATESTXT-VALUE !
END-WORD

\ Create a CREATEd word with the specified name, setting the word to have a data
\ pointer to the address in the data stack after where the word's name is
\ copied, setting the word to have a null secondary code pointer, setting the
\ word to be hidden, setting LATESTXT to the word, and adding the word to the
\ current wordlist
DEFINE-WORD CREATE-WITH-NAME ( c-addr bytes -- )
  ALLOCATE-STRING HERE NEW-CREATE DUP 3 LIT ROLL 3 LIT ROLL ROT
\  S" creating: " +DATA TYPE 2 LIT PICK 2 LIT PICK TYPE CR
  NAME>WORD DUP LATESTXT-VALUE !
  GET-CURRENT WORDLIST>FIRST OVER NEXT>WORD
  GET-CURRENT FIRST>WORDLIST
END-WORD

\ Parse a name and create a CREATEd word with the specified name, setting the
\ word to have a data pointer to the address in the data stack after where the
\ word's name is copied, setting the word to have a null secondary code pointer,
\ setting the word to be hidden, setting LATESTXT to the word, and adding the
\ word to the current wordlist
DEFINE-WORD CREATE ( -- )
  PARSE-NAME DUP 0 LIT <> +IF
    CREATE-WITH-NAME
  +ELSE
    2DROP &X-NO-PARSE-FOUND ?RAISE
  +THEN
END-WORD

\ Get the xt of the most recently created word
DEFINE-WORD LATESTXT ( -- xt ) LATESTXT-VALUE @ END-WORD

\ Specify that the most recently created word is immediate
DEFINE-WORD IMMEDIATE ( -- )
  LATESTXT WORD>FLAGS IMMEDIATE-FLAG LIT OR LATESTXT FLAGS>WORD
END-WORD

\ Specify that the most recently create word is compile-only
DEFINE-WORD COMPILE-ONLY ( -- )
  LATESTXT WORD>FLAGS COMPILE-ONLY-FLAG LIT OR LATESTXT FLAGS>WORD
END-WORD

\ Finish compilation of a word whose compilation was started with : or :NONAME ,
\ unsetting the hidden flag
NON-DEFINE-WORD-IMMEDIATE-COMPILE-ONLY ; ( -- )
  LATESTXT +IF
    &EXIT COMPILE, &END COMPILE,
    LATESTXT WORD>FLAGS HIDDEN-FLAG LIT NOT AND LATESTXT FLAGS>WORD
    LATESTXT FINISH
    +FALSE STATE !
  +ELSE
    &X-NO-LATESTXT ?RAISE
  +THEN
END-WORD

\ Set the secondary code pointer of LATESTXT to the pointer on the top of the
\ return stack and set the interpretation pointer to the pointer beneath it on
\ the return stack; an exception is raised if LATESTXT is unset
DEFINE-WORD DOES> ( -- )
  LATESTXT ?DUP +IF SET-DOES> +ELSE &X-NO-LATESTXT ?RAISE +THEN
END-WORD

\ Parse a name and create a cell-sized constant whose value is on the data stack
\ with it as its name
DEFINE-WORD CONSTANT ( x -- ) CREATE , DOES> @ END-WORD

\ Parse a name and create a cell-sized variable initialized to zero with it as
\ its name
DEFINE-WORD VARIABLE ( -- ) CREATE 0 LIT , END-WORD

\ Advance the pointer to a string by one byte and subtract one from its length
DEFINE-WORD ADVANCE-STRING ( c-addr1 bytes1 -- c-addr2 bytes2 )
  1 LIT - SWAP 1 LIT + SWAP
END-WORD

\ Parse the base for a numeric literal
DEFINE-WORD PARSE-BASE ( c-addr1 bytes1 -- c-addr2 bytes2 base )
  DUP 0 LIT > +IF
    OVER C@ DUP CHAR $ LIT = +IF DROP ADVANCE-STRING 16 LIT
    +ELSE DUP CHAR # LIT = +IF DROP ADVANCE-STRING 10 LIT
    +ELSE DUP CHAR / LIT = +IF DROP ADVANCE-STRING 8 LIT
    +ELSE CHAR % LIT = +IF ADVANCE-STRING 2 LIT
    +ELSE BASE @
    +THEN +THEN +THEN +THEN
  +ELSE
    BASE @
  +THEN
END-WORD

\ Parse a digit from 0 to 9 in a numeric literal
DEFINE-WORD PARSE-DIGIT-0-9 ( base c -- digit matches )
  CHAR 0 LIT - DUP ROT < +IF +TRUE +ELSE DROP 0 LIT +FALSE +THEN
END-WORD

\ Parse a digit from A to Z in a numeric literal
DEFINE-WORD PARSE-DIGIT-A-Z ( base c -- digit matches )
  CHAR A LIT - 10 LIT + DUP ROT < +IF +TRUE
  +ELSE DROP 0 LIT +FALSE +THEN
END-WORD

\ Parse a digit in a numeric literal
DEFINE-WORD PARSE-DIGIT ( base c -- digit matches )
  DUP CHAR 0 LIT >= OVER CHAR 9 LIT <= AND +IF
    PARSE-DIGIT-0-9
  +ELSE
    UPCASE-CHAR DUP CHAR A LIT >= OVER CHAR Z LIT <= AND +IF
      PARSE-DIGIT-A-Z
    +ELSE
      2DROP 0 LIT +FALSE
    +THEN
  +THEN
END-WORD

\ Parse the digits of a numeric literal
DEFINE-WORD PARSE-DIGITS ( base c-addr bytes -- n matches )
  0 LIT +BEGIN OVER 0 LIT > +WHILE
    3 LIT PICK 3 LIT PICK C@ PARSE-DIGIT +IF
      SWAP 4 LIT PICK * + ROT 1 LIT + ROT 1 LIT - ROT
    +ELSE
      2DROP 2DROP DROP 0 LIT +FALSE EXIT
    +THEN
  +REPEAT
  SWAP DROP SWAP DROP SWAP DROP +TRUE
END-WORD

\ Parse a numeric literal including an initial minus sign
DEFINE-WORD PARSE-NUMBER ( c-addr bytes -- n matches )
  PARSE-BASE ROT ROT DUP 0 LIT U> +IF
    OVER C@ CHAR - LIT = +IF
      1 LIT - SWAP 1 LIT + SWAP DUP 0 LIT U> +IF
        PARSE-DIGITS DUP +IF
	  DROP NEGATE +TRUE
	+THEN
      +ELSE
        2DROP DROP 0 LIT +FALSE
      +THEN
    +ELSE
      PARSE-DIGITS
    +THEN
  +ELSE
    2DROP DROP 0 LIT +FALSE
  +THEN
END-WORD

\ Immediately set the state to interpretation mode
DEFINE-WORD-IMMEDIATE [ ( -- ) +FALSE STATE ! END-WORD

\ Immediately set the state to compilation mode
DEFINE-WORD-IMMEDIATE ] ( -- ) +TRUE STATE ! END-WORD

\ Compile an xt unless it is immediate, where then it is executed immediately
DEFINE-WORD COMPILE-WORD ( c-addr bytes xt -- )
  ROT ROT 2DROP DUP WORD>FLAGS IMMEDIATE-FLAG LIT AND +IF
    EXECUTE
  +ELSE
    COMPILE,
  +THEN
END-WORD

\ Interpret an xt unless it is compile-only, where then an exception is raised
DEFINE-WORD INTERPRET-WORD ( c-addr bytes xt -- )
  DUP WORD>FLAGS COMPILE-ONLY-FLAG LIT AND +IF
    DROP SAVE-EXCEPTION &X-COMPILE-ONLY-ERROR ?RAISE
  +ELSE
    ROT ROT 2DROP EXECUTE
  +THEN
END-WORD

\ Parse a number, and in compilation mode compile it as a literal, unless it
\ cannot be parsed, where then an exception is raised
DEFINE-WORD HANDLE-NUMBER ( c-addr bytes -- )
  2DUP PARSE-NUMBER +IF
    ROT ROT 2DROP
    STATE @ +IF
      &(LIT) COMPILE, ,
    +THEN
  +ELSE
    DROP SAVE-EXCEPTION &X-PARSE-ERROR ?RAISE
  +THEN
END-WORD

\ Data stack underflow exception
DEFINE-WORD X-DATA-STACK-UNDERFLOW ( -- )
  SPACE S" data stack underflow" +DATA TYPE CR
END-WORD

\ Return stack underflow exception
DEFINE-WORD X-RETURN-STACK-UNDERFLOW ( -- )
  SPACE S" return stack underflow" +DATA TYPE CR
END-WORD

\ Flag indicating whether PAUSE is called for every word that is interpreted
DEFINE-WORD-CREATED PAUSE-ON-INTERPRET
TRUE SET-CELL-DATA

\ Interpret every word in the interpretation buffer, unless an exception is
\ raised or data stack or return stack underflow occur; if PAUSE-ON-INTERPRET
\ is set PAUSE is called for each word that is interpreted
DEFINE-WORD INTERPRET ( -- )
  +BEGIN
    PARSE-NAME DUP 0 LIT U> +IF
      2DUP SEARCH-WORDLISTS +IF
        STATE @ +IF
	  COMPILE-WORD
	+ELSE
          INTERPRET-WORD
	+THEN
	SP@ SBASE @ > +IF &X-DATA-STACK-UNDERFLOW ?RAISE +THEN
	RP@ RBASE @ > +IF &X-RETURN-STACK-UNDERFLOW ?RAISE +THEN
	+FALSE
      +ELSE
        DROP HANDLE-NUMBER +FALSE
      +THEN
    +ELSE
      DROP DROP +TRUE
    +THEN
    PAUSE-ON-INTERPRET @ +IF PAUSE +THEN
  +UNTIL
END-WORD

\ Evaluate code in a string by pushing the current interpretation buffer state
\ onto the data stack, pushing the string as the new interpretation buffer,
\ interpreting the code, restoring the interpretation buffer state (even if any
\ exceptions occur), and re-raising any exceptions that may have occurred
DEFINE-WORD EVALUATE ( c-addr bytes -- )
  INPUT @ >R INPUT# @ >R >IN @ >R
  0 LIT >IN ! INPUT# ! INPUT !
  &INTERPRET TRY R> >IN ! R> INPUT# ! R> INPUT ! ?DUP +IF ?RAISE +THEN
END-WORD

\ This is the interpretation buffer used for code gotten from the terminal
\ input buffer, to enable using ACCEPT while interpreting code read from the
\ terminal
DEFINE-WORD-CREATED INPUT-BUFFER
TIB-SIZE 0 SET-FILL-DATA

\ This is the number of bytes in the interpretation buffer used for code gotten
\ from the terminal input buffer
DEFINE-WORD-CREATED INPUT-BUFFER#
0 SET-CELL-DATA

\ Refill the interpretation buffer when input is set to the terminal
DEFINE-WORD REFILL-TERMINAL ( -- )
  INPUT-BUFFER TIB-SIZE LIT ACCEPT DUP INPUT-BUFFER# ! INPUT# !
  INPUT-BUFFER INPUT ! 0 LIT >IN !
END-WORD

\ Refill hook
DEFINE-WORD-CREATED 'REFILL
0 SET-CELL-DATA

\ Refill wrapper
DEFINE-WORD REFILL ( -- ) 'REFILL @ ?DUP +IF EXECUTE +THEN END-WORD

\ Abort exception
DEFINE-WORD ABORT-EXCEPTION ( -- ) SPACE S" aborted" +DATA TYPE CR END-WORD

\ Quit exception
DEFINE-WORD QUIT-EXCEPTION ( -- ) SPACE S" quit" +DATA TYPE CR END-WORD

\ Pointer to receive address of Forth code bundled in with a VM image
DEFINE-WORD-CREATED STORAGE
0 SET-CELL-DATA

\ Pointer to receive number of bytes of Forth code bundled in with a VM image
DEFINE-WORD-CREATED STORAGE#
0 SET-CELL-DATA

\ Execute an xt with single-tasking IO set to TRUE
DEFINE-WORD EXECUTE-SINGLE-TASK-IO ( xt -- )
  SINGLE-TASK-IO @ +TRUE SINGLE-TASK-IO ! SWAP TRY SINGLE-TASK-IO ! ?RAISE
END-WORD

\ Execute Forth code bundled in with a VM image
DEFINE-WORD INTERPRET-STORAGE ( -- )
  INPUT @ >R INPUT# @ >R >IN @ >R
  0 LIT >IN ! STORAGE# @ INPUT# ! STORAGE @ INPUT !
  &INTERPRET TRY R> >IN ! R> INPUT# ! R> INPUT ! ?DUP +IF
    DUP &QUIT-EXCEPTION = +IF
      EXECUTE-SINGLE-TASK-IO +FALSE STATE !
    +ELSE
      EXECUTE-SINGLE-TASK-IO SBASE @ SP! +FALSE STATE !
    +THEN
  +THEN
END-WORD

\ The main outer interpreter
DEFINE-WORD OUTER ( -- )
  RBASE @ RP!
  +BEGIN
    REFILL
    &INTERPRET TRY DUP 0 LIT = +IF
      DROP STATE @ 0 LIT = INPUT @ INPUT-BUFFER = AND +IF
        SPACE S" ok" +DATA TYPE CR
      +THEN
    +ELSE
      DUP &QUIT-EXCEPTION = +IF
        EXECUTE-SINGLE-TASK-IO RBASE @ RP! +FALSE STATE !
      +ELSE
        EXECUTE-SINGLE-TASK-IO SBASE @ SP! RBASE @ RP! +FALSE STATE !
      +THEN
    +THEN
    PAUSE
  +AGAIN
END-WORD

\ Raise an abort exception
DEFINE-WORD ABORT &ABORT-EXCEPTION ?RAISE END-WORD

\ Raise a quit exception
DEFINE-WORD QUIT &QUIT-EXCEPTION ?RAISE END-WORD

\ Parse a name and create a marker named by it, which when executed will erase
\ everything defined after and including itself, including restoring wordlists
\ to the state where they were when the marker was defined
DEFINE-WORD MARKER ( "name" -- )
  LATESTXT HERE CREATE , , WORDLIST-COUNT @ ,
  WORDLIST-ARRAY WORDLIST-COUNT @ +BEGIN DUP 0 LIT > +WHILE
    SWAP DUP @ DUP LATESTXT = +IF WORD>NEXT +THEN , CELL+ SWAP 1 LIT -
  +REPEAT
  2DROP WORDLIST-ORDER-COUNT @ ,
  WORDLIST-ORDER-ARRAY WORDLIST-ORDER-COUNT @ +BEGIN DUP 0 LIT > +WHILE
    SWAP DUP @ , CELL+ SWAP 1 LIT -
  +REPEAT
  2DROP GET-CURRENT ,
  DOES>
  DUP @ HERE! CELL+ DUP @ DUP LATESTXT-VALUE ! 1 LIT + SET-WORD-COUNT CELL+
  DUP @ DUP WORDLIST-COUNT !
  SWAP CELL+ SWAP WORDLIST-ARRAY SWAP +BEGIN DUP 0 LIT > +WHILE
    SWAP ROT DUP @ ROT TUCK ! CELL+ SWAP CELL+ SWAP ROT 1 LIT -
  +REPEAT
  2DROP DUP @ DUP WORDLIST-ORDER-COUNT !
  SWAP CELL+ SWAP WORDLIST-ORDER-ARRAY SWAP +BEGIN DUP 0 LIT > +WHILE
    SWAP ROT DUP @ ROT TUCK ! CELL+ SWAP CELL+ SWAP ROT 1 LIT -
  +REPEAT
  2DROP @ SET-CURRENT
END-WORD

\ Relocate the name table upon loading
DEFINE-WORD RELOCATE-NAME-TABLE ( end-token -- )
  0 LIT +BEGIN 2DUP >= +WHILE
    NAME-TABLE OVER 2 LIT CELLS * NAME-TABLE + +! 1 LIT +
  +REPEAT
  2DROP
END-WORD

\ Exception-protected boot actions
DEFINE-WORD BOOT-PROTECTED ( -- )
  LOOKUP-SYS NAME-TABLE SET-NAME-TABLE
  SET-HOOKS &REFILL-TERMINAL 'REFILL ! INTERPRET-STORAGE OUTER
END-WORD

\ The initial entry point
DEFINE-WORD BOOT ( storage storage-size here -- )
  USER-SPACE-CURRENT ! STORAGE# ! STORAGE !
  SP@ SBASE ! RP@ RBASE ! INPUT-BUFFER INPUT !
  0 LIT INPUT-BUFFER# ! 0 LIT INPUT# !
  &BOOT RELOCATE-NAME-TABLE
  &BOOT WORDLIST-ARRAY 0 LIT CELLS + ! &BOOT-PROTECTED TRY 0 LIT <> +IF
    S" error" +DATA STDOUT WRITE 2DROP BYE
  +ELSE
    BYE
  +THEN
END-WORD

\ Forth files to bundle in with the assembled VM
S" src/hashforth/startup.fs" ADD-SOURCE-TO-STORAGE
S" src/hashforth/buffer.fs" ADD-SOURCE-TO-STORAGE
S" src/hashforth/lambda.fs" ADD-SOURCE-TO-STORAGE
S" src/hashforth/task.fs" ADD-SOURCE-TO-STORAGE
S" src/hashforth/io.fs" ADD-SOURCE-TO-STORAGE
S" src/hashforth/cond.fs" ADD-SOURCE-TO-STORAGE
S" src/hashforth/mutex.fs" ADD-SOURCE-TO-STORAGE
S" src/hashforth/bchan.fs" ADD-SOURCE-TO-STORAGE
S" src/hashforth/bufbchan.fs" ADD-SOURCE-TO-STORAGE
S" src/hashforth/varbchan.fs" ADD-SOURCE-TO-STORAGE
