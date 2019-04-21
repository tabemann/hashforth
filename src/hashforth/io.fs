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
FORTH-WORDLIST TASK-WORDLIST BUFFER-WORDLIST 3 SET-ORDER
FORTH-WORDLIST SET-CURRENT

\ Read-only file opening flag
1 CONSTANT OPEN-RDONLY

\ Write-only file opening flag
2 CONSTANT OPEN-WRONLY

\ Read/write file opening flag
4 CONSTANT OPEN-RDWR

\ Appending file opening flag
8 CONSTANT OPEN-APPEND

\ Creating file opening flag
16 CONSTANT OPEN-CREAT

\ Exclusive create (to be combined with OPEN-CREAT) file opening flag
32 CONSTANT OPEN-EXCL

\ Truncating file opening flag
64 CONSTANT OPEN-TRUNC

\ Services for opening and closing files.
VARIABLE SYS-OPEN
VARIABLE SYS-CLOSE
VARIABLE SYS-PREPARE-TERMINAL
VARIABLE SYS-CLEANUP-TERMINAL
VARIABLE SYS-GET-TERMINAL-SIZE

\ Call service for opening files; returns -1 on success and 0 on failure
: OPEN ( c-addr bytes flags mode -- fd -1|0 ) SYS-OPEN @ SYS ;

\ Call service for closing files; returns -1 on success and 0 on failure
: CLOSE ( fd -- -1|0 ) SYS-CLOSE @ SYS ;

\ Call service for preparing terminals; returns -1 on success and 0 on failure
: PREPARE-TERMINAL ( fd -- -1|0 ) SYS-PREPARE-TERMINAL @ SYS ;

\ Call service for cleaning up terminals; returns -1 on success and 0 on failure
: CLEANUP-TERMINAL ( fd -- -1|0 ) SYS-CLEANUP-TERMINAL @ SYS ;

\ Call service to get the terminal size; returns -1 on success and 0 on failure
: GET-TERMINAL-SIZE ( fd -- rows columns xpixels ypixels -1|0 )
  SYS-GET-TERMINAL-SIZE @ SYS ;

\ Hook for preparing to read
VARIABLE 'PREPARE-READ

\ Wrapper for preparing to read
: PREPARE-READ ( fd -- ) 'PREPARE-READ @ ?DUP IF EXECUTE ELSE DROP THEN ;

\ Actually wait for and read a file descriptor (returns -1 on success and 0 on
\ error).
: (WAIT-READ) ( buf bytes fd -- bytes-read -1|0 )
  DUP SET-WAIT-IN BEGIN    
    PAUSE
    2 PICK 2 PICK 2 PICK READ DUP TRUE = IF
      DROP SWAP DROP SWAP DROP SWAP DROP TRUE TRUE
    ELSE 1 = IF
      DROP FALSE
    ELSE
      2DROP 2DROP 0 FALSE TRUE
    THEN THEN
  UNTIL
  UNSET-WAIT-IN ;

\ Wait for and read a file descriptor (returns -1 on success and 0 on error).
: WAIT-READ ( buf bytes fd -- bytes-read -1|0 )
  DUP GET-NONBLOCKING DROP >R >R R@ TRUE OVER SET-NONBLOCKING IF
    2 PICK 2 PICK 2 PICK
    READ DUP TRUE = IF
      DROP SWAP DROP SWAP DROP SWAP DROP TRUE
    ELSE 1 = IF
      DROP (WAIT-READ)
    ELSE
      2DROP 2DROP 0
    THEN THEN
  ELSE
    DROP 2DROP 0
  THEN
  R> R> SWAP SET-NONBLOCKING 0 = IF 2DROP 0 0 THEN ;

\ Attempt to fully read a buffer of data from a file descriptor (returns -1 on
\ success and 0 on error).
: WAIT-READ-FULL ( buf bytes fd -- bytes-read -1|0 )
  IN-TASK? AVERTS X-NOT-IN-TASK
  DUP PREPARE-READ
  0 BEGIN
    3 PICK 3 PICK 3 PICK WAIT-READ IF
      DUP 0 = IF
        DROP -1 TRUE
      ELSE
        TUCK + 4 ROLL 4 ROLL 3 ROLL ADVANCE-BUFFER 3 ROLL 3 ROLL
        2 PICK 0 = IF -1 TRUE ELSE FALSE THEN
      THEN
    ELSE
      DROP 0 TRUE
    THEN
  UNTIL
  ROT DROP ROT DROP ROT DROP ;

\ Wait for and read a file descriptor (returns -1 on success and 0 on error).
: WAIT-READ ( buf bytes fd -- bytes-read -1|0 )
  IN-TASK? AVERTS X-NOT-IN-TASK DUP PREPARE-READ WAIT-READ ;

\ Actually wait for and write a file descriptor (returns -1 on success and 0 on
\ error).
: (WAIT-WRITE) ( buf bytes fd -- bytes-written -1|0 )
  DUP SET-WAIT-OUT BEGIN
    PAUSE
    2 PICK 2 PICK 2 PICK WRITE DUP TRUE = IF
      DROP SWAP DROP SWAP DROP SWAP DROP TRUE TRUE
    ELSE 1 = IF
      FALSE
    ELSE
      2DROP 2DROP 0 FALSE TRUE
    THEN THEN
  UNTIL
  UNSET-WAIT-OUT ;

\ Wait for and write a file descriptor (returns -1 on success and 0 on error).
: WAIT-WRITE ( buf bytes fd -- bytes-written -1|0 )
  IN-TASK? AVERTS X-NOT-IN-TASK
  DUP GET-NONBLOCKING IF
    >R >R R@ TRUE OVER SET-NONBLOCKING IF
      2 PICK 2 PICK 2 PICK WRITE DUP TRUE = IF
        DROP SWAP DROP SWAP DROP SWAP DROP TRUE
      ELSE 1 = IF
        DROP (WAIT-WRITE)
      ELSE
        2DROP 2DROP 0 0
      THEN THEN
    ELSE
      DROP 2DROP 0 0
    THEN
  ELSE
    DROP 2DROP 0 0
  THEN
  R> R> SWAP SET-NONBLOCKING 0 = IF 2DROP 0 0 THEN ;

\ Attempt to fully write a buffer of data from a file descriptor (returns -1 on
\ success and 0 on error).
: WAIT-WRITE-FULL ( buf bytes fd -- bytes-write -1|0 )
  IN-TASK? AVERTS X-NOT-IN-TASK
  0 BEGIN
    3 PICK 3 PICK 3 PICK WAIT-WRITE IF
      TUCK + 4 ROLL 4 ROLL 3 ROLL ADVANCE-BUFFER 3 ROLL 3 ROLL
      2 PICK 0 = IF -1 TRUE ELSE FALSE THEN
    ELSE
      DROP 0 TRUE
    THEN
  UNTIL
  ROT DROP ROT DROP ROT DROP ;

\ Implement TYPE
: (TYPE) ( c-addr bytes -- )
  IN-TASK? SINGLE-TASK-IO @ NOT AND IF
    OUTPUT-FD @ WAIT-WRITE-FULL AVERTS X-UNABLE-TO-WRITE-STDOUT DROP
  ELSE
    (TYPE)
  THEN ;

\ Test whether a key is read.
: (KEY?) ( -- flag )
  IN-TASK? SINGLE-TASK-IO @ NOT AND IF
    READ-KEY? @ IF
      TRUE
    ELSE
      INPUT-FD @ GET-NONBLOCKING AVERTS X-UNABLE-TO-READ-STDIN
      TRUE INPUT-FD @ SET-NONBLOCKING AVERTS X-UNABLE-TO-READ-STDIN
      HERE 1 1 ALLOT INPUT-FD @ READ DUP 0<> AVERTS X-UNABLE-TO-READ-STDIN
      -1 = IF
        -1 ALLOT 1 = IF HERE C@ READ-KEY ! TRUE READ-KEY? ! TRUE
        ELSE ['] X-UNABLE-TO-READ-STDIN ?RAISE THEN
      ELSE
        -1 ALLOT DROP FALSE
      THEN
      SWAP INPUT-FD @ SET-NONBLOCKING AVERTS X-UNABLE-TO-READ-STDIN
    THEN
  ELSE
    (KEY?)
  THEN ;

\ Read a keypress from standard input.
: (KEY) ( -- c )
  IN-TASK? SINGLE-TASK-IO @ NOT AND IF
    READ-KEY? @ IF
      READ-KEY @ FALSE READ-KEY? !
    ELSE
      HERE 1 1 ALLOT INPUT-FD @ WAIT-READ-FULL AVERTS X-UNABLE-TO-READ-STDIN
      -1 ALLOT 1 = IF HERE C@ ELSE BYE THEN
    THEN
  ELSE
    (KEY)
  THEN ;

\ Buffer size for reading data into memory
1024 CONSTANT READ-BUFFER-SIZE
CREATE READ-BUFFER READ-BUFFER-SIZE ALLOT

\ Read a whole file into memory in a buffer; returns -1 on success and 0 on
\ failure.
: READ-FILE-INTO-BUFFER ( c-addr bytes buffer -- -1|0 )
  ROT ROT OPEN-RDONLY /777 OPEN IF
    BEGIN
      READ-BUFFER READ-BUFFER-SIZE 2 PICK WAIT-READ-FULL IF
        DUP 0 = IF
          DROP -1 TRUE
        ELSE
          READ-BUFFER SWAP 3 PICK APPEND-BUFFER FALSE
        THEN
      ELSE
        DROP 0 TRUE
      THEN
    UNTIL
    SWAP CLOSE DROP SWAP DROP
  ELSE
    DROP DROP 0
  THEN ;

\ Unable to load Forth code exception.
: X-CODE-LOADING-FAILURE SPACE ." unable to load code" CR ;

\ Default included code buffer size
1024 CONSTANT INCLUDED-BUFFER-SIZE

\ Linked list item for previously included source files
BEGIN-STRUCTURE INCLUDED-ITEM-SIZE
  FIELD: INCLUDED-ITEM-NEXT
  FIELD: INCLUDED-ITEM-NAME-LEN
END-STRUCTURE

\ Included source file name
: INCLUDED-ITEM-NAME ( addr -- ) INCLUDED-ITEM-SIZE + ;

\ First included item
VARIABLE FIRST-INCLUDED-ITEM
 
\ Allocate an included item
: INCLUDE-ITEM ( c-addr bytes -- )
  INCLUDED-ITEM-SIZE ALLOCATE! FIRST-INCLUDED-ITEM @ OVER INCLUDED-ITEM-NEXT !
  2DUP INCLUDED-ITEM-NAME-LEN !
  ROT ROT 2 PICK INCLUDED-ITEM-NAME SWAP CMOVE
  FIRST-INCLUDED-ITEM ! ;

\ Get whether a file has already been included
: INCLUDED? ( c-addr bytes -- included-flag )
  FIRST-INCLUDED-ITEM BEGIN
    DUP IF
      DUP INCLUDED-ITEM-NAME OVER INCLUDED-ITEM-NAME-LEN @
      4 PICK 4 PICK EQUAL-STRINGS? IF
        DROP 2DROP TRUE TRUE
      ELSE
        INCLUDED-ITEM-NEXT @ FALSE
      THEN
    ELSE
      DROP 2DROP FALSE TRUE
    THEN
  UNTIL ;

\ Add an included item if it is not already included.
: CHECK-INCLUDE-ITEM ( c-addr bytes -- )
  2DUP INCLUDED? IF INCLUDE-ITEM ELSE 2DROP THEN ;

\ Load Forth code from a file.
: INCLUDED ( c-addr bytes -- )
  2DUP CHECK-INCLUDE-ITEM
  INCLUDED-BUFFER-SIZE NEW-BUFFER
  ROT ROT 2 PICK READ-FILE-INTO-BUFFER AVERTS X-CODE-LOADING-FAILURE
  DUP GET-BUFFER EVALUATE DESTROY-BUFFER ;

\ Load Forth code from a file specified in interpretation mode.
: INCLUDE ( "file" -- ) PARSE-NAME INCLUDED ; IMMEDIATE

\ Load Forth code from a file if it is not already loaded.
: REQUIRED ( c-addr bytes -- ) 2DUP INCLUDED? NOT IF INCLUDED ELSE 2DROP THEN ;

\ Load Forth code from a file specified in interpretation mode if it is not
\ already loaded.
: REQUIRE ( "file" -- ) PARSE-NAME REQUIRED ; IMMEDIATE

\ Execute code with a specific input file descriptor
: WITH-INPUT-FD ( xt fd -- )
  INPUT-FD @ >R INPUT-FD ! TRY R> INPUT-FD ! ?RAISE ;

\ Execute code with a specific output file descriptor
: WITH-OUTPUT-FD ( xt fd -- )
  OUTPUT-FD @ >R OUTPUT-FD ! TRY R> OUTPUT-FD ! ?RAISE ;

\ Execute code with a specific error file descriptor
: WITH-ERROR-FD ( xt fd -- )
  ERROR-FD @ >R ERROR-FD ! TRY R> ERROR-FD ! ?RAISE ;

\ Old BYE implementation
VARIABLE OLD-BYE 'BYE @ OLD-BYE !

\ New BYE implementation
: (BYE) ( -- ) INPUT-FD @ CLEANUP-TERMINAL OLD-BYE @ EXECUTE ;

\ Initialize IO
: INIT-IO ( -- )
  S" OPEN" SYS-LOOKUP SYS-OPEN !
  S" CLOSE" SYS-LOOKUP SYS-CLOSE !
  S" PREPARE-TERMINAL" SYS-LOOKUP SYS-PREPARE-TERMINAL !
  S" CLEANUP-TERMINAL" SYS-LOOKUP SYS-CLEANUP-TERMINAL !
  S" GET-TERMINAL-SIZE" SYS-LOOKUP SYS-GET-TERMINAL-SIZE !
  ['] (BYE) 'BYE !
  ['] (TYPE) 'TYPE !
  ['] (KEY?) 'KEY? !
  ['] (KEY) 'KEY !
  ['] (ACCEPT) 'ACCEPT ! ;

INIT-IO

BASE ! SET-CURRENT SET-ORDER
