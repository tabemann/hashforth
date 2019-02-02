\ Copyright (c) 2018-2019, Travis Bemann
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
FORTH-WORDLIST TASK-WORDLIST 2 SET-ORDER
TASK-WORDLIST SET-CURRENT

BEGIN-STRUCTURE COND-SIZE
  FIELD: COND-HELD
  FIELD: COND-WAITING
  FIELD: COND-WAITING-COUNT
  FIELD: COND-WAITING-SIZE
END-STRUCTURE

\ Allocate a new condition variable.
: NEW-COND ( waiting-size -- cond )
  HERE COND-SIZE ALLOT
  0 OVER COND-HELD !
  SWAP OVER COND-WAITING-SIZE !
  0 OVER COND-WAITING-COUNT !
  DUP COND-WAITING-SIZE @ HERE SWAP CELLS ALLOT OVER COND-WAITING ! ;

\ Wait on a condition variable.
: WAIT-COND ( cond -- )
  BEGIN DUP COND-WAITING-COUNT @ OVER COND-WAITING-SIZE @ = WHILE
    PAUSE
  REPEAT
  DUP COND-WAITING @ OVER COND-WAITING-COUNT @ + CURRENT-TASK SWAP !
  DUP COND-WAITING-COUNT @ 1 + SWAP COND-WAITING-COUNT !
  CURRENT-TASK DEACTIVATE-TASK ;

\ Signal a condition variable.
: SIGNAL-COND ( cond -- )
  DUP COND-WAITING-COUNT @ 0 > IF
    DUP COND-WAITING @ @ ACTIVATE-TASK
    DUP COND-WAITING @ CELL+ OVER COND-WAITING @
    2 PICK COND-WAITING-COUNT @ 1 - CELLS MOVE
    DUP COND-WAITING-COUNT @ 1 - SWAP COND-WAITING-COUNT !
  ELSE
    DROP
  THEN ;

\ Broadcast on a condition variable.
: BROADCAST-COND ( cond -- )
  DUP COND-WAITING-COUNT @ 0 ?DO
    DUP COND-WAITING @ I CELLS + @ ACTIVATE-TASK
  LOOP
  0 SWAP COND-WAITING-COUNT ! ;

BASE ! SET-CURRENT SET-ORDER
