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

forth-wordlist vector-wordlist 2 set-order

4 2 cells allocate-vector constant my-vector-0
4 2 cells allocate-vector constant my-vector-1

true constant success
false constant failure

: type-pair ( value1 value 2 -- ) ." ( " SWAP . ." , " . ." ) " ;

: dump-vector ( vector -- )
  >r ." < " 0 begin dup r@ count-vector < while
    dup r@ get-vector-2cell if type-pair else 2drop ." ??? " then 1 +
  repeat
  drop ." > " r> drop ;

: push-start-entry ( success value1 value2 vector -- )
  ." pushing-start " 2 pick 2 pick type-pair
  dup >r push-start-vector-2cell not xor
  if ." success " else ." failure " then r> dump-vector cr ;

: push-end-entry ( success value1 value2 vector -- )
  ." pushing-end " 2 pick 2 pick type-pair
  dup >r push-end-vector-2cell not xor
  if ." success " else ." failure " then r> dump-vector cr ;

: pop-start-entry ( success vector -- )
  ." popping-start " dup >r pop-start-vector-2cell rot not xor
  if type-pair ." success " else 2drop ." failure " then r> dump-vector cr ;

: pop-end-entry ( success vector -- )
  ." popping-end " dup >r pop-end-vector-2cell rot not xor
  if type-pair ." success " else 2drop ." failure " then r> dump-vector cr ;

: peek-start-entry ( success vector -- )
  ." peeking-start " dup >r peek-start-vector-cell rot not xor
  if type-pair ." success " else 2drop ." failure " then r> dump-vector cr ;

: peek-end-entry ( success vector -- )
  ." peeking-end " dup >r peek-end-vector-cell rot not xor
  if type-pair ." success " else 2drop ." failure " then r> dump-vector cr ;

: drop-start-entry ( success vector -- )
  ." dropping-start " dup >r drop-start-vector not xor
  if ." success " else 2drop ." failure " then r> dump-vector cr ;

: drop-end-entry ( success vector -- )
  ." dropping-end " dup >r drop-end-vector not xor
  if ." success " else 2drop ." failure " then r> dump-vector cr ;

: prepend-entries ( success source-vector dest-vector )
  ." prepending " dup >r prepend-vector not xor
  if ." success " else ." failure " then r> dump-vector cr ;

: append-entries ( success source-vector dest-vector )
  ." appending " dup >r append-vector not xor
  if ." success " else ." failure " then r> dump-vector cr ;

: clear-entries ( vector -- ) ." clearing " dup clear-vector dump-vector cr ;

cr .( Populating MY-VECTOR-0) cr

success 0 -1 my-vector-0 push-start-entry
success 1 -2 my-vector-0 push-end-entry
success 2 -3 my-vector-0 push-start-entry
success 3 -4 my-vector-0 push-end-entry
success 4 -5 my-vector-0 push-start-entry
success 5 -6 my-vector-0 push-end-entry

.( Copying to MY-VECTOR-1) cr

success 0 0 my-vector-1 push-start-entry
success my-vector-0 my-vector-1 prepend-entries
success my-vector-0 my-vector-1 append-entries

.( Popping entries from MY-VECTOR-1) cr

success my-vector-1 pop-start-entry
success my-vector-1 pop-end-entry
success my-vector-1 pop-start-entry
success my-vector-1 pop-end-entry
success my-vector-1 pop-start-entry
success my-vector-1 pop-end-entry

.( Dropping entries from MY-VECTOR-1) cr

success my-vector-1 drop-start-entry
success my-vector-1 drop-end-entry
success my-vector-1 drop-start-entry
success my-vector-1 drop-end-entry
success my-vector-1 drop-start-entry
success my-vector-1 drop-end-entry
success my-vector-1 drop-end-entry
