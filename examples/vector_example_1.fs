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

INCLUDE src/hashforth/vector.fs

FORTH-WORDLIST VECTOR-WORDLIST 2 SET-ORDER

4 1 CELLS ALLOCATE-VECTOR CONSTANT MY-VECTOR-0
4 1 CELLS ALLOCATE-VECTOR CONSTANT MY-VECTOR-1

TRUE CONSTANT SUCCESS
FALSE CONSTANT FAILURE

: DUMP-VECTOR ( vector -- )
  >R ." < " 0 BEGIN DUP R@ COUNT-VECTOR < WHILE
    DUP R@ GET-VECTOR-CELL IF . ELSE DROP ." ??? " THEN 1 +
  REPEAT
  DROP ." > " R> DROP ;

: PUSH-START-ENTRY ( success value vector -- )
  ." pushing-start " OVER . DUP >R PUSH-START-VECTOR-CELL NOT XOR
  IF ." success " ELSE ." failure " THEN R> DUMP-VECTOR CR ;

: PUSH-END-ENTRY ( success value vector -- )
  ." pushing-end " OVER . DUP >R PUSH-END-VECTOR-CELL NOT XOR
  IF ." success " ELSE ." failure " THEN R> DUMP-VECTOR CR ;

: POP-START-ENTRY ( success vector -- )
  ." popping-start " DUP >R POP-START-VECTOR-CELL ROT NOT XOR
  IF . ." success " ELSE DROP ." failure " THEN R> DUMP-VECTOR CR ;

: POP-END-ENTRY ( success vector -- )
  ." popping-end " DUP >R POP-END-VECTOR-CELL ROT NOT XOR
  IF . ." success " ELSE DROP ." failure " THEN R> DUMP-VECTOR CR ;

: PEEK-START-ENTRY ( success vector -- )
  ." peeking-start " DUP >R PEEK-START-VECTOR-CELL ROT NOT XOR
  IF . ." success " ELSE DROP ." failure " THEN R> DUMP-VECTOR CR ;

: PEEK-END-ENTRY ( success vector -- )
  ." peeking-end " DUP >R PEEK-END-VECTOR-CELL ROT NOT XOR
  IF . ." success " ELSE DROP ." failure " THEN R> DUMP-VECTOR CR ;

: DROP-START-ENTRY ( success vector -- )
  ." dropping-start " DUP >R DROP-START-VECTOR NOT XOR
  IF ." success " ELSE DROP ." failure " THEN R> DUMP-VECTOR CR ;

: DROP-END-ENTRY ( success vector -- )
  ." dropping-end " DUP >R DROP-END-VECTOR NOT XOR
  IF ." success " ELSE DROP ." failure " THEN R> DUMP-VECTOR CR ;

: PREPEND-ENTRIES ( success source-vector dest-vector )
  ." prepending " DUP >R PREPEND-VECTOR NOT XOR
  IF ." success " ELSE DROP ." failure " THEN R> DUMP-VECTOR CR ;

: APPEND-ENTRIES ( success source-vector dest-vector )
  ." appending " DUP >R APPEND-VECTOR NOT XOR
  IF ." success " ELSE DROP ." failure " THEN R> DUMP-VECTOR CR ;

: CLEAR-ENTRIES ( vector -- ) ." clearing " DUP CLEAR-VECTOR DUMP-VECTOR CR ;

CR .( Populating MY-VECTOR-0) CR

SUCCESS 0 MY-VECTOR-0 PUSH-START-ENTRY
SUCCESS 1 MY-VECTOR-0 PUSH-END-ENTRY
SUCCESS 2 MY-VECTOR-0 PUSH-START-ENTRY
SUCCESS 3 MY-VECTOR-0 PUSH-END-ENTRY
SUCCESS 4 MY-VECTOR-0 PUSH-START-ENTRY
SUCCESS 5 MY-VECTOR-0 PUSH-END-ENTRY

.( Copying to MY-VECTOR-1) CR

SUCCESS -1 MY-VECTOR-1 PUSH-START-ENTRY
SUCCESS MY-VECTOR-0 MY-VECTOR-1 PREPEND-ENTRIES
SUCCESS MY-VECTOR-0 MY-VECTOR-1 APPEND-ENTRIES

.( Popping entries from MY-VECTOR-1) CR

SUCCESS MY-VECTOR-1 POP-START-ENTRY
SUCCESS MY-VECTOR-1 POP-END-ENTRY
SUCCESS MY-VECTOR-1 POP-START-ENTRY
SUCCESS MY-VECTOR-1 POP-END-ENTRY
SUCCESS MY-VECTOR-1 POP-START-ENTRY
SUCCESS MY-VECTOR-1 POP-END-ENTRY

.( Dropping entries from MY-VECTOR-1) CR

SUCCESS MY-VECTOR-1 DROP-START-ENTRY
SUCCESS MY-VECTOR-1 DROP-END-ENTRY
SUCCESS MY-VECTOR-1 DROP-START-ENTRY
SUCCESS MY-VECTOR-1 DROP-END-ENTRY
SUCCESS MY-VECTOR-1 DROP-START-ENTRY
SUCCESS MY-VECTOR-1 DROP-END-ENTRY
SUCCESS MY-VECTOR-1 DROP-END-ENTRY
