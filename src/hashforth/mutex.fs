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

BEGIN-STRUCTURE MUTEX-SIZE
  FIELD: MUTEX-HELD
  FIELD: MUTEX-WAITING
  FIELD: MUTEX-WAITING-COUNT
  FIELD: MUTEX-WAITING-SIZE
END-STRUCTURE

\ Allocate a new mutex variable with a specified waiting queue size.
: NEW-MUTEX ( waiting-size -- mutex )
  HERE MUTEX-SIZE ALLOT
  TUCK MUTEX-WAITING-SIZE !
  0 OVER MUTEX-WAITING-COUNT !
  0 OVER MUTEX-HELD !
  DUP MUTEX-WAITING-SIZE @ HERE SWAP CELLS ALLOT OVER MUTEX-WAITING ! ;

\ Lock a mutex.
: LOCK-MUTEX ( mutex -- )
  BEGIN DUP MUTEX-HELD @ 0<> OVER MUTEX-HELD @ CURRENT-TASK <> AND WHILE
    BEGIN DUP MUTEX-WAITING-COUNT @ OVER MUTEX-WAITING-SIZE @ = WHILE
      PAUSE
    REPEAT
    DUP MUTEX-HELD @ 0<> AND MUTEX-HELD @ CURRENT-TASK <> AND IF
      DUP MUTEX-WAITING @ OVER MUTEX-WAITING-COUNT @ CELLS + CURRENT-TASK SWAP !
      1 SWAP MUTEX-WAITING-COUNT +!
      CURRENT-TASK DEACTIVATE-TASK
    THEN
  REPEAT
  CURRENT-TASK SWAP MUTEX-HELD ! ;

\ Unlock a mutex.
: UNLOCK-MUTEX ( mutex -- )
  DUP MUTEX-HELD @ CURRENT-TASK = IF
    DUP MUTEX-WAITING-COUNT @ 0 > IF
      DUP MUTEX-WAITING @ @ 2DUP MUTEX-HELD ! ACTIVATE-TASK
      DUP MUTEX-WAITING @ CELL+ OVER MUTEX-WAITING @
      2 PICK MUTEX-WAITING-COUNT @ 1 - CELLS MOVE
      -1 SWAP MUTEX-WAITING-COUNT +!
    ELSE
      0 SWAP MUTEX-HELD !
    THEN
  THEN ;

\ Lock and unlock a mutex after executing a word.
: WITH-MUTEX ( mutex xt -- )
  OVER LOCK-MUTEX SWAP >R TRY R> UNLOCK-MUTEX ?RAISE ;

BASE ! SET-CURRENT SET-ORDER