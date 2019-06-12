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

get-order get-current base @

decimal
forth-wordlist 1 set-order
forth-wordlist set-current

wordlist constant vector-wordlist
forth-wordlist vector-wordlist 2 set-order
vector-wordlist set-current

wordlist constant vector-private-wordlist
forth-wordlist vector-private-wordlist vector-wordlist
lambda-wordlist 4 set-order
vector-private-wordlist set-current

\ The vector structure
begin-structure vector-size
  \ The vector flags
  field: vector-flags
  
  \ The vector circular buffer
  field: vector-data

  \ The number of entries in the vector
  field: vector-count

  \ The maximum number of entries in the vector (without resizing)
  field: vector-max-count

  \ The size of an entry in the vector
  field: vector-entry-size

  \ The current start index in the circular buffer
  field: vector-start-index

  \ The current end index in the circular buffer
  field: vector-end-index

  \ The finalizer word
  field: vector-finalizer

  \ THe extra finalizer argument
  field: vector-finalizer-arg
end-structure

\ The vector-is-allocated flag
1 constant allocated-vector

\ Internal - get address of a block at an index in a vector.
: get-vector-entry ( index vector -- addr )
  2dup vector-start-index @ swap <= if
    dup vector-start-index @ rot 1 + - over vector-max-count @ +
  else
    dup vector-start-index @ rot 1 + -
  then
  over vector-entry-size @ * swap vector-data @ + ;

\ Carry out finalizing of an entry if there is a finalizer xt
: do-finalize ( index vector -- )
  dup vector-finalizer @ if
    tuck get-vector-entry over vector-finalizer-arg @ rot
    vector-finalizer @ execute
  else
    2drop
  then ;

\ Internal - wrap an index backward
: wrap-back ( index vector -- index )
  swap 1- dup 0 < if swap vector-max-count @ + else nip then ;

\ Internal - get a block at an index in a vector.
: get-vector ( addr index vector -- )
  2dup vector-start-index @ swap <= if
    dup vector-start-index @ rot 1 + - over vector-max-count @ +
  else
    dup vector-start-index @ rot 1 + -
  then
  over vector-entry-size @ * over vector-data @ +
  rot rot vector-entry-size @ cmove ;

\ Internal - set a block at an index in a vector.
: set-vector ( addr index vector -- )
  2dup vector-start-index @ swap < if
    dup vector-start-index @ rot 1 + - over vector-max-count @ +
  else
    dup vector-start-index @ rot 1 + -
  then
  over vector-entry-size @ * over vector-data @ +
  rot swap rot vector-entry-size @ cmove ;

\ Internal - push a block onto the start of a vector.
: push-start-vector ( addr vector -- )
  tuck vector-data @ 2 pick vector-start-index @
  3 pick vector-entry-size @ * + 2 pick vector-entry-size @ cmove
  dup vector-count @ 1+ over vector-count !
  dup vector-start-index @ 1+ over vector-max-count @ mod
  swap vector-start-index ! ;

\ Internal - pop a block from the start of a vector.
: pop-start-vector ( addr vector -- )
  dup vector-start-index @ over wrap-back over vector-start-index !
  dup vector-data @ over vector-start-index @
  2 pick vector-entry-size @ * + rot 2 pick vector-entry-size @ cmove
  dup vector-count @ 1- swap vector-count ! ;

\ Internal - drop a block from the start of a vector.
: drop-start-vector ( vector -- )
  dup vector-start-index @ over wrap-back over vector-start-index !
  dup vector-count @ 1- swap vector-count ! ;

\ Internal - peek a block from the start a vector.
: peek-start-vector ( addr vector -- )
  dup vector-data @ over vector-start-index @ 2 pick wrap-back
  2 pick vector-entry-size @ * + rot rot vector-entry-size @ cmove ;

\ Internal - push a block onto the start of a vector.
: push-end-vector ( addr vector -- )
  dup vector-end-index @ over wrap-back over vector-end-index !
  tuck vector-data @ 2 pick vector-end-index @
  3 pick vector-entry-size @ * + 2 pick vector-entry-size @ cmove
  dup vector-count @ 1+ swap vector-count ! ;

\ Internal - pop a block from the end of a vector.
: pop-end-vector ( addr vector -- )
  dup vector-data @ over vector-end-index @
  2 pick vector-entry-size @ * + rot 2 pick vector-entry-size @ cmove
  dup vector-count @ 1- over vector-count !
  dup vector-end-index @ 1+ over vector-max-count @ mod
  swap vector-end-index ! ;

\ Internal - drop a block from the end of a vector.
: drop-end-vector ( vector -- )
  dup vector-count @ 1- over vector-count !
  dup vector-end-index @ 1+ over vector-max-count @ mod
  swap vector-end-index ! ;

\ Internal - peek a block from the end a vector.
: peek-end-vector ( addr vector -- )
  dup vector-data @ over vector-end-index @
  2 pick vector-entry-size @ * + rot rot vector-entry-size @ cmove ;

\ Prepend a vector to another vector
: prepend-vector ( source-vector dest-vector -- )
  over vector-entry-size @ here swap allot
  2 pick vector-count @ 0 ?do
    dup 3 pick vector-count @ 1 - i - 4 pick get-vector
    dup 2 pick push-start-vector
  loop
  over vector-entry-size @ negate allot
  2drop drop ;

\ Append a vector to another vector
: append-vector ( source-vector dest-vector -- )
  over vector-entry-size @ here swap allot
  2 pick vector-count @ 0 ?do
    dup i 4 pick get-vector
    dup 2 pick push-end-vector
  loop
  over vector-entry-size @ negate allot
  2drop drop ;
  
\ Expand a vector
: expand-vector ( vector -- success )
  here vector-size allot
  0 over vector-flags !
  0 over vector-count !
  over vector-max-count @ 2 * over vector-max-count !
  over vector-entry-size @ over vector-entry-size !
  0 over vector-start-index !
  0 over vector-end-index !
  0 over vector-finalizer !
  0 over vector-finalizer-arg !
  dup vector-max-count @ over vector-entry-size @ * allocate if
    over vector-data !
    2dup append-vector
    dup vector-start-index @ 2 pick vector-start-index !
    dup vector-end-index @ 2 pick vector-end-index !
    dup vector-max-count @ 2 pick vector-max-count !
    over vector-data @ free! vector-data @ swap vector-data !
    vector-size negate allot true
  else
    2drop drop vector-size negate allot false
  then ;

\ Expand a vector to fit multiple entries
: expand-multiple ( count vector -- success )
  dup vector-max-count @ 2 pick < if
    dup vector-flags @ allocated-vector and if
      here vector-size allot
      0 over vector-flags !
      0 over vector-count !
      rot 2 pick vector-max-count @ 2 * max over vector-max-count !
      over vector-entry-size @ over vector-entry-size !
      0 over vector-start-index !
      0 over vector-end-index !
      0 over vector-finalizer !
      0 over vector-finalizer-arg !
      dup vector-max-count @ over vector-entry-size @ * allocate if
	over vector-data !
	2dup append-vector
	dup vector-start-index @ 2 pick vector-start-index !
	dup vector-end-index @ 2 pick vector-end-index !
	dup vector-max-count @ 2 pick vector-max-count !
	over vector-data @ free! vector-data @ swap vector-data !
	vector-size negate allot true
      else
	2drop drop vector-size negate allot false
      then
    else
      2drop false
    then
  else
    2drop true
  then ;

vector-wordlist set-current

\ Print out the internal values of a vector.
: vector. ( vector -- )
  cr ." vector-data: " dup vector-data @ .
  cr ." vector-flags: " dup vector-flags @ .
  cr ." vector-count: " dup vector-count @ .
  cr ." vector-max-count: " dup vector-max-count @ .
  cr ." vector-entry-size: " dup vector-entry-size @ .
  cr ." vector-start-index: " dup vector-start-index @ .
  cr ." vector-end-index: " vector-end-index @ .
  cr ." vector-finalizer: " vector-finalizer @ .
  cr ." vector-finalizer-arg: " vector-finalizer-arg @ . cr ;

\ Vector is not allocated exception
: x-vector-not-allocated ( -- ) space ." vector is not allocated" cr ;

\ Allot a new vector with the specified entry count and entry size.
: allot-vector ( entry-count entry-size -- vector )
  here vector-size allot
  2 pick over vector-max-count !
  0 over vector-flags !
  0 over vector-count !
  0 over vector-start-index !
  0 over vector-end-index !
  0 over vector-finalizer !
  0 over vector-finalizer-arg !
  here 3 roll 3 pick * allot over vector-data !
  tuck vector-entry-size ! ;

\ Allocate a new vector with the specified entry count and entry size.
: allocate-vector ( entry-count entry-size -- vector )
  vector-size allocate!
  2 pick over vector-max-count !
  allocated-vector over vector-flags !
  0 over vector-count !
  0 over vector-start-index !
  0 over vector-end-index !
  0 over vector-finalizer !
  0 over vector-finalizer-arg !
  rot 2 pick * allocate! over vector-data !
  tuck vector-entry-size ! ;

\ Set a finalizer
: set-vector-finalizer ( finalizer finalizer-arg map -- )
  tuck vector-finalizer-arg ! vector-finalizer ! ;

\ Destroy an allocated vector.
: destroy-vector ( vector -- )
  dup vector-flags @ allocated-vector = averts x-vector-not-allocated
  dup vector-data @ free! free! ;

\ Clear a vector.
: clear-vector ( vector -- )
  0 over vector-start-index ! 0 over vector-end-index ! 0 swap vector-count ! ;

\ Get the number of blocks queued in a vector.
: count-vector ( vector -- u ) vector-count @ ;

\ Get whether a vector is empty.
: empty-vector? ( vector -- empty ) vector-count @ 0 = ;

\ Get whether a vector is full.
: full-vector? ( vector -- full )
  dup vector-count @ swap vector-max-count @ = ;

\ Vector is not composed of cells exception
: x-non-cell-vector ( -- ) space ." non-cell vector" cr ;

\ Vector is not composed of double cells exception
: x-non-2cell-vector ( -- ) space ." non-double cell vector" cr ;

\ Evaluate an xt for the address of each member of a vector from left to right;
\ note that the internal state is hidden from the xt, so the xt can
\ transparently access the outside stack
: iter-left-vector ( xt vector -- )
  0 begin
    dup 2 pick count-vector < if
      2dup swap get-vector-entry swap >r swap >r swap >r r@ execute r> r> r> 1 +
      false
    else
      drop 2drop true
    then
  until ;

\ Evaluate an xt for the address of each member of a vector from right to left;
\ note that the internal state is hidden from the xt, so the xt can
\ transparently access the outside stack
: iter-right-vector ( xt vector -- )
  dup count-vector 1 - begin
    dup 0 >= if
      2dup swap get-vector-entry swap >r swap >r swap >r r@ execute r> r> r> 1 -
      false
    else
      drop 2drop true
    then
  until ;

\ Evaluate an xt for the cell of each member of a vector from left to right;
\ note that the internal state is hidden from the xt, so the xt can
\ transparently access the outside stack
: iter-left-vector-cell ( xt vector -- )
  dup vector-entry-size @ 1 cells = averts x-non-cell-vector
  [: @ swap dup >r execute r> ;] swap iter-left-vector drop ;

\ Evaluate an xt for the cell of each member of a vector from right to left;
\ note that the internal state is hidden from the xt, so the xt can
\ transparently access the outside stack
: iter-right-vector-cell ( xt vector -- )
  dup vector-entry-size @ 1 cells = averts x-non-cell-vector
  [: @ swap dup >r execute r> ;] swap iter-right-vector drop ;

\ Evaluate an xt for the double cell of each member of a vector from left to
\ right; note that the internal state is hidden from the xt, so the xt can
\ transparently access the outside stack
: iter-left-vector-2cell ( xt vector -- )
  dup vector-entry-size @ 2 cells = averts x-non-2cell-vector
  [: 2@ rot dup >r execute r> ;] swap iter-left-vector drop ;

\ Evaluate an xt for the double cell of each member of a vector from right to
\ left; note that the internal state is hidden from the xt, so the xt can
\ transparently access the outside stack
: iter-right-vector-2cell ( xt vector -- )
  dup vector-entry-size @ 2 cells = averts x-non-2cell-vector
  [: 2@ rot dup >r execute r> ;] swap iter-right-vector drop ;

\ Get a block at an index in a vector and return whether it was successful.
: get-vector ( addr index vector -- success )
  2dup count-vector < if get-vector true else 2drop drop false then ;

\ Get a cell at an index in a vector and return whether it was successful.
: get-vector-cell ( index vector -- value found )
  dup vector-entry-size @ 1 cells = averts x-non-cell-vector
  here 1 cells allot dup >r rot rot get-vector if
    r> @ true
  else
    r> drop 0 false
  then
  -1 cells allot ;

\ Get a double cell at an index in a vector and return whether it was
\ successful.
: get-vector-2cell ( index vector -- value1 value2 found )
  dup vector-entry-size @ 2 cells = averts x-non-2cell-vector
  here 2 cells allot dup >r rot rot get-vector if
    r> 2@ true
  else
    r> drop 0 0 false
  then
  -2 cells allot ;

\ Set a block at an index in a vector and return whether it was successful.
: set-vector ( addr index vector -- success )
  2dup count-vector < if
    2dup do-finalize set-vector true
  else
    2drop drop false
  then ;

\ Set a cell at an index in a vector and return whether it was successful.
: set-vector-cell ( value index vector -- success )
  dup vector-entry-size @ 1 cells = averts x-non-cell-vector
  rot here 1 cells allot tuck ! rot rot set-vector -1 cells allot ;

\ Set a double cell at an index in a vector and return whether it was
\ successful.
: set-vector-2cell ( value1 value2 index vector -- success )
  dup vector-entry-size @ 2 cells = averts x-non-2cell-vector
  >r >r here rot rot 2 pick 2! 2 cells allot
  r> r> set-vector -2 cells allot ;

\ Push a block onto the start of a vector and return whether it was successful.
: push-start-vector ( addr vector -- success )
  dup full-vector? not if
    push-start-vector true
  else
    dup vector-flags @ allocated-vector and if
      dup expand-vector if
	push-start-vector true
      else
	2drop false
      then
    else
      2drop false
    then
  then ;

\ Push a cell onto the start of a vector and return whether it was successful.
: push-start-vector-cell ( value vector -- success )
  dup vector-entry-size @ 1 cells = averts x-non-cell-vector
  here 1 cells allot rot over ! swap push-start-vector -1 cells allot ;

\ Push a double cell onto the start of a vector and return whether it was
\ successful.
: push-start-vector-2cell ( value1 value2 vector -- success )
  dup vector-entry-size @ 2 cells = averts x-non-2cell-vector
  rot rot here dup >r 2 cells allot 2! r> swap push-start-vector
  -2 cells allot ;

\ Pop a block from the start of a vector and return whether it was successful.
: pop-start-vector ( addr vector -- success )
  dup empty-vector? not if pop-start-vector true else 2drop false then ;

\ Pop a cell from the start of a vector and return whether it was successful.
: pop-start-vector-cell ( vector -- value success )
  dup vector-entry-size @ 1 cells = averts x-non-cell-vector
  here 1 cells allot tuck swap pop-start-vector if @ true else drop 0 false then
  -1 cells allot ;

\ Pop a double cell from the start of a vector and return whether it was
\ successful.
: pop-start-vector-2cell ( vector -- value1 value2 success )
  dup vector-entry-size @ 2 cells = averts x-non-2cell-vector
  here 2 cells allot tuck swap pop-start-vector if
    2@ true
  else
    drop 0 0 false
  then
  -2 cells allot ;

\ Drop a block from the start of a vector and return whether it was successful.
: drop-start-vector ( vector -- success )
  dup empty-vector? not if
    0 over do-finalize drop-start-vector true
  else
    drop false
  then ;

\ Peek a block from the start of a vector and return whether it was successful.
: peek-start-vector ( addr vector -- success )
  dup empty-vector? not if peek-start-vector true else 2drop false then ;

\ Peek a cell from the start of a vector and return whether it was successful.
: peek-start-vector-cell ( vector -- value success )
  dup vector-entry-size @ 1 cells = averts x-non-cell-vector
  here 1 cells allot tuck swap peek-start-vector
  if @ true else drop 0 false then
  -1 cells allot ;

\ Peek a double cell from the start of a vector and return whether it was
\ successful.
: peek-start-vector-2cell ( vector -- value1 value2 success )
  dup vector-entry-size @ 2 cells = averts x-non-2cell-vector
  here 2 cells allot tuck swap peek-start-vector if
    2@ true
  else
    drop 0 0 false
  then
  -2 cells allot ;

\ Push a block onto the end of a vector and return whether it was successful.
: push-end-vector ( addr vector -- success )
  dup full-vector? not if
    push-end-vector true
  else
    dup vector-flags @ allocated-vector and if
      dup expand-vector if
	push-end-vector true
      else
	2drop false
      then
    else
      2drop false
    then
  then ;

\ Push a cell onto the end of a vector and return whether it was successful.
: push-end-vector-cell ( value vector -- success )
  dup vector-entry-size @ 1 cells = averts x-non-cell-vector
  here 1 cells allot rot over ! swap push-end-vector -1 cells allot ;

\ Push a double cell onto the end of a vector and return whether it was
\ successful.
: push-end-vector-2cell ( value1 value2 vector -- success )
  dup vector-entry-size @ 2 cells = averts x-non-2cell-vector
  rot rot here dup >r 2 cells allot 2! r> swap push-end-vector
  -2 cells allot ;

\ Pop a block from the end of a vector and return whether it was successful.
: pop-end-vector ( addr vector -- success )
  dup empty-vector? not if pop-end-vector true else 2drop false then ;

\ Pop a cell from the end of a vector and return whether it was successful.
: pop-end-vector-cell ( vector -- value success )
  dup vector-entry-size @ 1 cells = averts x-non-cell-vector
  here 1 cells allot tuck swap pop-end-vector if @ true else drop 0 false then
  -1 cells allot ;

\ Pop a double cell from the end of a vector and return whether it was
\ successful.
: pop-end-vector-2cell ( vector -- value1 value2 success )
  dup vector-entry-size @ 2 cells = averts x-non-2cell-vector
  here 2 cells allot tuck swap pop-end-vector if
    2@ true
  else
    drop 0 0 false
  then
  -2 cells allot ;

\ Drop a block from the end of a vector and return whether it was successful.
: drop-end-vector ( vector -- success )
  dup empty-vector? not if
    dup count-vector 1 - over do-finalize drop-end-vector true
  else
    drop false
  then ;

\ Peek a block from the end of a vector and return whether it was successful.
: peek-end-vector ( addr vector -- success )
  dup empty-vector? not if peek-end-vector true else 2drop false then ;

\ Peek a cell from the end of a vector and return whether it was successful.
: peek-end-vector-cell ( vector -- value success )
  dup vector-entry-size @ 1 cells = averts x-non-cell-vector
  here 1 cells allot tuck swap peek-end-vector if @ true else drop 0 false then
  -1 cells allot ;

\ Peek a double cell from the end of a vector and return whether it was
\ successful.
: peek-end-vector-2cell ( vector -- value1 value2 success )
  dup vector-entry-size @ 2 cells = averts x-non-2cell-vector
  here 2 cells allot tuck swap peek-end-vector if
    2@ true
  else
    drop 0 0 false
  then
  -2 cells allot ;

\ Mismatched entry sizes exception
: x-entry-size-mismatch ( -- ) space ." vector entry size mismatch" cr ;

\ Prepend a vector to another vector
: prepend-vector ( source-vector dest-vector -- success )
  over vector-entry-size @ over vector-entry-size @ =
  averts x-entry-size-mismatch
  over vector-count @ over vector-count @ + over expand-multiple if
    prepend-vector true
  else
    2drop false
  then ;

\ Append a vector to another vector
: append-vector ( source-vector dest-vector -- success )
  over vector-entry-size @ over vector-entry-size @ =
  averts x-entry-size-mismatch
  over vector-count @ over vector-count @ + over expand-multiple if
    append-vector true
  else
    2drop false
  then ;

\ Get vector entry size.
: get-vector-entry-size ( vector -- u ) vector-entry-size @ ;

\ Get vector maximum count.
: get-vector-max-count ( vector -- u ) vector-max-count @ ;

base ! set-current set-order