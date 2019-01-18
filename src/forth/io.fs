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

WORDLIST CURRENT IO-WORDLIST

FORTH-WORDLIST TASK-WORDLIST IO-WORDLIST 3 SET-ORDER
IO-WORDLIST SET-CURRENT

\ Standard input
0 CONSTANT STDIN
1 CONSTANT STDOUT
2 CONSTANT STDERR

\ Services for IO
VARIABLE SYS-READ
VARIABLE SYS-WRITE
VARIABLE SYS-GET-NONBLOCKING
VARIABLE SYS-SET-NONBLOCKING

\ Read a file descriptor (a return value of -1 means success, a return value of
\ 0 means error, and a return value of 1 means that accessing would block were
\ it not for non-blocking).
: READ ( c-addr bytes fd -- bytes-read -1|0|1 ) SYS-READ @ SYS ;

\ Write a file descriptor (a return value of -1 means success, a return value of
\ 0 means error, and a return value of 1 means that accessing would block were
\ it not for non-blocking).
: WRITE ( c-addr bytes fd -- bytes-written -1|0|1 ) SYS-WRITE @ SYS ;

\ Get whether a file descriptor is non-blocking (a return value of -1 means
\ success and a return value 0f 0 means error).
: GET-NONBLOCKING ( fd -- non-blocking -1|0 ) SYS-GET-NONBLOCKING @ SYS ;

\ Get whether a file descriptor is blocking (a return value of -1 means success
\ and a return value of 0 means error).
: SET-BLOCKING ( non-blocking fd -- -1|0 ) SYS-SET-NONBLOCKING @ SYS ;

\ Actually wait for and read a file descriptor (returns -1 on success and 0 on
\ error).
: (WAIT-READ) ( buf bytes fd -- bytes-read -1|0 )
  DUP CURRENT-TASK @ SET-WAIT-IN BEGIN
    PAUSE
    2 PICK 2 PICK 2 PICK READ DUP TRUE = IF
      DROP SWAP DROP SWAP DROP SWAP DROP TRUE TRUE
    ELSE 1 = IF
      FALSE
    ELSE
      2DROP 2DROP 0 FALSE TRUE
    THEN THEN
  UNTIL
  CURRENT-TASK @ UNSET-WAIT-IN ;

\ Wait for and read a file descriptor (returns -1 on success and 0 on error).
: WAIT-READ ( buf bytes fd -- bytes-read -1|0 )
  IN-TASK? AVERTS X-NOT-IN-TASK
  DUP GET-NONBLOCKING >R >R R@ TRUE OVER SET-NONBLOCKING IF
    2 PICK 2 PICK 2 PICK READ DUP TRUE = IF
      DROP SWAP DROP SWAP DROP SWAP DROP TRUE
    ELSE 1 = IF
      DROP (WAIT-READ)
    ELSE
      2DROP 2DROP 0
    THEN
  ELSE
    DROP 2DROP 0
  THEN
  R> R> SWAP SET-NONBLOCKING ;

\ Advance a pointer and size for a buffer.
: ADVANCE-BUFFER ( c-addr bytes bytes-to-advance -- c-addr bytes )
  ROT OVER + ROT ROT - ;

\ Attempt to fully read a buffer of data from a file descriptor (returns -1 on
\ success and 0 on error).
: WAIT-READ-FULL ( buf bytes fd -- bytes-read -1|0 )
  IN-TASK? AVERTS X-NOT-IN-TASK
  0 BEGIN
    3 PICK 3 PICK 3 PICK WAIT-READ IF
      DUP 0 = IF
        DROP -1 TRUE
      ELSE
        TUCK + SWAP 4 ROLL 4 ROLL ROT ADVANCE-BUFFER 3 ROLL 3 ROLL
	2 PICK 0 = IF -1 TRUE ELSE FALSE THEN
      THEN
    ELSE
      DROP 0 TRUE
    THEN
  UNTIL
  ROT DROP ROT DROP ROT DROP ;

\ Actually wait for and write a file descriptor (returns -1 on success and 0 on
\ error).
: (WAIT-WRITE) ( buf bytes fd -- bytes-written -1|0 )
  DUP CURRENT-TASK @ SET-WAIT-OUT BEGIN
    PAUSE
    2 PICK 2 PICK 2 PICK WRITE DUP TRUE = IF
      DROP SWAP DROP SWAP DROP SWAP DROP TRUE TRUE
    ELSE 1 = IF
      FALSE
    ELSE
      2DROP 2DROP 0 FALSE TRUE
    THEN THEN
  UNTIL
  CURRENT-TASK @ UNSET-WAIT-OUT ;

\ Wait for and write a file descriptor (returns -1 on success and 0 on error).
: WAIT-WRITE ( buf bytes fd -- bytes-written -1|0 )
  IN-TASK? AVERTS X-NOT-IN-TASK
  DUP GET-NONBLOCKING >R >R R@ TRUE OVER SET-NONBLOCKING IF
    2 PICK 2 PICK 2 PICK WRITE DUP TRUE = IF
      DROP SWAP DROP SWAP DROP SWAP DROP TRUE
    ELSE 1 = IF
      DROP (WAIT-WRITE)
    ELSE
      2DROP 2DROP 0
    THEN
  ELSE
    DROP 2DROP 0
  THEN
  R> R> SWAP SET-NONBLOCKING ;

\ Attempt to fully write a buffer of data from a file descriptor (returns -1 on
\ success and 0 on error).
: WAIT-WRITE-FULL ( buf bytes fd -- bytes-write -1|0 )
  IN-TASK? AVERTS X-NOT-IN-TASK
  0 BEGIN
    3 PICK 3 PICK 3 PICK WAIT-WRITE IF
      TUCK + SWAP 4 ROLL 4 ROLL ROT ADVANCE-BUFFER 3 ROLL 3 ROLL
      2 PICK 0 = IF -1 TRUE ELSE FALSE THEN
    ELSE
      DROP 0 TRUE
    THEN
  UNTIL
  ROT DROP ROT DROP ROT DROP;

\ Error writing to standard output
: X-UNABLE-TO-WRITE-STDOUT SPACE S" unable to write to standard output" CR ;

\ Implement TYPE
: TYPE ( c-addr bytes -- )
  IN-TASK? IF
    STDOUT WAIT-WRITE-FULL AVERTS X-UNABLE-TO-WRITE-STDOUT DROP
  ELSE
    STDOUT GET-BLOCKING AVERTS X-UNABLE-TO-WRITE-STDOUT
    TRUE STDOUT SET-BLOCKING AVERTS X-UNABLE-TO-WRITE-STDOUT
    ROT ROT BEGIN DUP 0 > WHILE
      2DUP STDOUT WRITE AVERTS X-UNABLE-TO-WRITE-STDOUT ADVANCE-BUFFER
    REPEAT
    2DROP STDOUT SET-BLOCKING AVERTS X-UNABLE-TO-WRITE-STDOUT
  THEN ;

\ Standard input is closed
: X-STDIN-IS-CLOSED SPACE S" standard input is closed" CR ;

\ Error reading from standard input
: X-UNABLE-TO-READ-STDIN SPACE S" unable to read from standard input" CR ;

\ Standard input is supposed to be blocking
: X-IS-SUPPOSED-TO-BLOCK SPACE S" standard input is supposed to block" CR ;

\ Currently read character
VARIABLE READ-KEY

\ Whether a key has been read
VARIABLE READ-KEY? 0 READ-KEY? !

\ Test whether a key is read.
: KEY? ( -- flag )
  READ-KEY? @ IF
    TRUE
  ELSE
    STDIN GET-NONBLOCKING AVERTS X-UNABLE-TO-READ-STDIN
    TRUE STDIN SET-NONBLOCKING AVERTS X-UNABLE-TO-READ-STDIN
    THERE 1 1 TALLOT STDIN READ DUP 0<> AVERTS X-UNABLE-TO-READ-STDIN -1 = IF
      -1 TALLOT 1 = IF THERE C@ READ-KEY ! TRUE
      ELSE ['] X-UNABLE-TO-READ-STDIN ?RAISE THEN
    ELSE
      -1 TALLOT FALSE
    THEN
    SWAP STDIN SET-NONBLOCKING AVERTS X-UNABLE-TO-READ-STDIN
  THEN ;

\ Actually read a keypress from standard input.
: (KEY) ( -- c )
  THERE 1 1 TALLOT STDIN READ DUP AVERTS X-UNABLE-TO-READ-STDIN -1 = IF
     -1 TALLOT 1 = IF THERE C@ ELSE ['] X-UNABLE-TO-READ-STDIN ?RAISE THEN
  ELSE
    ['] X-IS-SUPPOSED-TO-BLOCK ?RAISE
  THEN ;

\ Read a keypress from standard input.
: KEY ( -- c )
  READ-KEY? @ IF
    READ-KEY @ FALSE READ-KEY? !
  ELSE
    IN-TASK? IF
      THERE 1 1 TALLOT STDIN WAIT-READ-FULL AVERTS X-UNABLE-TO-READ-STDIN
      -1 TALLOT 1 = IF THERE C@ ELSE ['] X-UNABLE-TO-READ-STDIN ?RAISE THEN
    ELSE
      STDIN GET-NONBLOCKING AVERTS X-UNABLE-TO-READ-STDIN
      FALSE STDIN SET-NONBLOCKING AVERTS X-UNABLE-TO-READ-STDIN (KEY)
      SWAP STDIN SET-NONBLOCKING AVERTS X-UNABLE-TO-READ-STDIN      
    THEN
  THEN ;

\ Read terminal input into a buffer
: ACCEPT ( c-addr bytes -- bytes-read )
  0 TIB# !
  TIB-SIZE 0 ?DO
    KEY DUP TIB TIB# @ + C! NEWLINE = IF LEAVE ELSE TIB# @ 1 + TIB# ! THEN
  LOOP
  DUP TIB# @ > IF DROP TIB# @ THEN
  SWAP TIB 2 PICK CMOVE ;

\ Initialize IO
: INIT-IO ( -- )
  S" READ" SYS-LOOKUP SYS-READ !
  S" WRITE" SYS-LOOKUP SYS-WRITE !
  S" GET-NONBLOCKING" SYS-LOOKUP SYS-GET-NONBLOCKING !
  S" SET-NONBLOCKING" SYS-LOOKUP SYS-SET-NONBLOCKING !
  ['] TYPE 'TYPE !
  ['] KEY 'KEY !
  ['] ACCEPT 'ACCEPT ! ;

INIT-IO

BASE ! SET-CURRENT SET-ORDER