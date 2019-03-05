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
FORTH-WORDLIST 1 SET-ORDER
FORTH-WORDLIST SET-CURRENT

WORDLIST CONSTANT DEQUE-WORDLIST
FORTH-WORDLIST DEQUE-WORDLIST 2 SET-ORDER
DEQUE-WORDLIST SET-CURRENT

BEGIN-STRUCTURE DEQUE-SIZE
  FIELD: DEQUE-DATA
  FIELD: DEQUE-COUNT
  FIELD: DEQUE-MAX-COUNT
  FIELD: DEQUE-ENTRY-SIZE
  FIELD: DEQUE-START-INDEX
  FIELD: DEQUE-END-INDEX
END-STRUCTURE

\ Print out the internal values of a deque.
: DEQUE. ( deque -- )
  CR ." DEQUE-DATA: " DUP DEQUE-DATA @ .
  CR ." DEQUE-COUNT: " DUP DEQUE-COUNT @ .
  CR ." DEQUE-MAX-COUNT: " DUP DEQUE-MAX-COUNT @ .
  CR ." DEQUE-ENTRY-SIZE: " DUP DEQUE-ENTRY-SIZE @ .
  CR ." DEQUE-START-INDEX: " DUP DEQUE-START-INDEX @ .
  CR ." DEQUE-END-INDEX: " DEQUE-END-INDEX @ . CR ;

\ Create a new deque with the specified queue size, entry size, and
\ condition variable queue size.
: NEW-DEQUE ( queue-size entry-size -- deque )
  HERE DEQUE-SIZE ALLOT
  2 PICK OVER DEQUE-MAX-COUNT !
  0 OVER DEQUE-COUNT !
  0 OVER DEQUE-START-INDEX !
  0 OVER DEQUE-END-INDEX !
  HERE 3 ROLL 3 PICK * ALLOT OVER DEQUE-DATA !
  TUCK DEQUE-ENTRY-SIZE ! ;

\ Clear a deque.
: CLEAR-DEQUE ( deque -- )
  0 OVER DEQUE-START-INDEX ! 0 OVER DEQUE-END-INDEX ! 0 SWAP DEQUE-COUNT ! ;

\ Internal - wrap an index backward
: WRAP-BACK ( index deque -- index )
  SWAP 1- DUP 0 < IF SWAP DEQUE-MAX-COUNT @ + ELSE NIP THEN ;

\ Internal - get a block at an index in a deque.
: GET-DEQUE ( addr index deque -- )
  2DUP DEQUE-START-INDEX @ SWAP <= IF
    DUP DEQUE-START-INDEX @ ROT 1 + - OVER DEQUE-MAX-COUNT @ +
  ELSE
    DUP DEQUE-START-INDEX @ ROT 1 + -
  THEN
  OVER DEQUE-ENTRY-SIZE @ * OVER DEQUE-DATA @ +
  ROT ROT DEQUE-ENTRY-SIZE @ CMOVE ;

\ Internal - set a block at an index in a deque.
: SET-DEQUE ( addr index deque -- )
  2DUP DEQUE-START-INDEX @ SWAP < IF
    DUP DEQUE-START-INDEX @ ROT 1 + - OVER DEQUE-MAX-COUNT @ +
  ELSE
    DUP DEQUE-START-INDEX @ ROT 1 + -
  THEN
  OVER DEQUE-ENTRY-SIZE @ * OVER DEQUE-DATA @ +
  ROT SWAP ROT DEQUE-ENTRY-SIZE @ CMOVE ;

\ Internal - push a block onto the start of a deque.
: PUSH-START-DEQUE ( addr deque -- )
  TUCK DEQUE-DATA @ 2 PICK DEQUE-START-INDEX @
  3 PICK DEQUE-ENTRY-SIZE @ * + 2 PICK DEQUE-ENTRY-SIZE @ CMOVE
  DUP DEQUE-COUNT @ 1+ OVER DEQUE-COUNT !
  DUP DEQUE-START-INDEX @ 1+ OVER DEQUE-MAX-COUNT @ MOD
  SWAP DEQUE-START-INDEX ! ;

\ Internal - pop a block from the start of a deque.
: POP-START-DEQUE ( addr deque -- )
  DUP DEQUE-START-INDEX @ OVER WRAP-BACK OVER DEQUE-START-INDEX !
  DUP DEQUE-DATA @ OVER DEQUE-START-INDEX @
  2 PICK DEQUE-ENTRY-SIZE @ * + ROT 2 PICK DEQUE-ENTRY-SIZE @ CMOVE
  DUP DEQUE-COUNT @ 1- SWAP DEQUE-COUNT ! ;

\ Internal - peek a block from the start a deque.
: PEEK-START-DEQUE ( addr deque -- )
  DUP DEQUE-DATA @ OVER DEQUE-START-INDEX @ 2 PICK WRAP-BACK
  2 PICK DEQUE-ENTRY-SIZE @ * + ROT ROT DEQUE-ENTRY-SIZE @ CMOVE ;

\ Internal - push a block onto the start of a deque.
: PUSH-END-DEQUE ( addr deque -- )
  DUP DEQUE-END-INDEX @ OVER WRAP-BACK OVER DEQUE-END-INDEX !
  TUCK DEQUE-DATA @ 2 PICK DEQUE-END-INDEX @
  3 PICK DEQUE-ENTRY-SIZE @ * + 2 PICK DEQUE-ENTRY-SIZE @ CMOVE
  DUP DEQUE-COUNT @ 1+ SWAP DEQUE-COUNT ! ;

\ Internal - pop a block from the end of a deque.
: POP-END-DEQUE ( addr deque -- )
  DUP DEQUE-DATA @ OVER DEQUE-END-INDEX @
  2 PICK DEQUE-ENTRY-SIZE @ * + ROT 2 PICK DEQUE-ENTRY-SIZE @ CMOVE
  DUP DEQUE-COUNT @ 1- OVER DEQUE-COUNT !
  DUP DEQUE-END-INDEX @ 1+ OVER DEQUE-MAX-COUNT @ MOD
  SWAP DEQUE-END-INDEX ! ;

\ Internal - peek a block from the end a deque.
: PEEK-END-DEQUE ( addr deque -- )
  DUP DEQUE-DATA @ OVER DEQUE-END-INDEX @
  2 PICK DEQUE-ENTRY-SIZE @ * + ROT ROT DEQUE-ENTRY-SIZE @ CMOVE ;

\ Get the number of blocks queued in a deque.
: COUNT-DEQUE ( deque -- u ) DEQUE-COUNT @ ;

\ Get whether a deque is empty.
: EMPTY-DEQUE? ( deque -- empty ) DEQUE-COUNT @ 0 = ;

\ Get whether a deque is full.
: FULL-DEQUE? ( deque -- full )
  DUP DEQUE-COUNT @ SWAP DEQUE-MAX-COUNT @ = ;

\ Get a block at an index in a deque and return whether it was successful.
: GET-DEQUE ( addr index deque -- success )
  2DUP COUNT-DEQUE < IF GET-DEQUE TRUE ELSE 2DROP DROP FALSE THEN ;

\ Attempt to set a block at an index in a deque and return whether it was
\ successful.
: SET-DEQUE ( addr index deque -- success )
  2DUP COUNT-DEQUE < IF SET-DEQUE TRUE ELSE 2DROP DROP FALSE THEN ;

\ Push a block onto the start of a deque and return whether it was successful.
: PUSH-START-DEQUE ( addr deque -- success )
  DUP FULL-DEQUE? NOT IF PUSH-START-DEQUE TRUE ELSE 2DROP FALSE THEN ;

\ Pop a block from the start of a deque and return whether it was successful.
: POP-START-DEQUE ( addr deque -- success )
  DUP EMPTY-DEQUE? NOT IF POP-START-DEQUE TRUE ELSE 2DROP FALSE THEN ;

\ Peek a block from the start of a deque and return whether it was successful.
: PEEK-START-DEQUE ( addr deque -- success )
  DUP EMPTY-DEQUE? NOT IF PEEK-START-DEQUE TRUE ELSE 2DROP FALSE THEN ;

\ Push a block onto the end of a deque and return whether it was successful.
: PUSH-END-DEQUE ( addr deque -- success )
  DUP FULL-DEQUE? NOT IF PUSH-END-DEQUE TRUE ELSE 2DROP FALSE THEN ;

\ Pop a block from the end of a deque and return whether it was successful.
: POP-END-DEQUE ( addr deque -- success )
  DUP EMPTY-DEQUE? NOT IF POP-END-DEQUE TRUE ELSE 2DROP FALSE THEN ;

\ Peek a block from the end of a deque and return whether it was successful.
: PEEK-END-DEQUE ( addr deque -- success )
  DUP EMPTY-DEQUE? NOT IF PEEK-END-DEQUE TRUE ELSE 2DROP FALSE THEN ;

\ Get deque entry size.
: GET-DEQUE-ENTRY-SIZE ( deque -- u ) DEQUE-ENTRY-SIZE @ ;

\ Get deque maximum count.
: GET-DEQUE-MAX-COUNT ( deque -- u ) DEQUE-MAX-COUNT @ ;

BASE ! SET-CURRENT SET-ORDER