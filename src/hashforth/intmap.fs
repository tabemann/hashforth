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

get-order get-current base @

decimal
forth-wordlist 1 set-order
forth-wordlist set-current

wordlist constant intmap-wordlist
forth-wordlist lambda-wordlist intmap-wordlist 3 set-order
intmap-wordlist set-current

wordlist constant intmap-private-wordlist
forth-wordlist lambda-wordlist intmap-private-wordlist intmap-wordlist
lambda-wordlist 5 set-order
intmap-private-wordlist set-current

\ The intmap entry structure
begin-structure intmap-header-size
  \ The intmap entry key (+ 1, a key of 0 indicates that an entry is free)
  field: intmap-entry-key
end-structure

\ The intmap structure
begin-structure intmap-size
  \ The intmap flags
  field: intmap-flags

  \ The intmap value size
  field: intmap-value-size
  
  \ The number of intmap entry fields, regardless of whether they are used
  field: intmap-count

  \ The number of actual intmap entries
  field: intmap-entry-count

  \ The intmap entries
  field: intmap-entries

  \ The finalizer word
  field: intmap-finalizer

  \ The extra finalizer argument
  field: intmap-finalizer-arg
end-structure

\ Get the intmap entry size
: intmap-entry-size ( intmap -- bytes )
  intmap-value-size @ intmap-header-size + ;

\ Convert a key into an entry key
: get-entry-key ( key intmap -- entry-key ) intmap-count @ umod ;

\ Get entry address
: get-entry ( entry-key intmap -- entry )
  dup intmap-entries @ rot rot intmap-entry-size * + ;

\ Get the index for a key; returns -1 if key is not found
: get-index ( key intmap -- index )
  2dup get-entry-key dup >r begin
    dup 2 pick get-entry
    dup intmap-entry-key @ 0 = if ( key intmap entry-key entry )
      drop nip nip r> drop true ( [: ." a " ;] as-non-task-io )
    else
      dup intmap-entry-key @ 4 pick 1 + = if
	drop nip nip r> drop true ( [: ." b " ;] as-non-task-io )
      else
	drop 1 + over intmap-count @ umod dup r@ <> if
	  false ( [: ." c " ;] as-non-task-io )
	else
	  2drop drop r> drop -1 true ( [: ." d " ;] as-non-task-io )
	then
      then
    then
  until ;

\ Get whether an index is a found index
: is-found-index ( index intmap -- ) get-entry intmap-entry-key @ 0 <> ;

\ Carry out finalizing of an entry if there is a finalizer xt
: do-finalize ( entry intmap -- )
  dup >r intmap-finalizer @ if
    dup intmap-header-size + swap intmap-entry-key @
    r@ intmap-finalizer-arg @ r> intmap-finalizer @ execute
  else
    drop r> drop
  then ;

\ The intmap allocated flag
1 constant intmap-allocated

\ Actually set a value in an intmap
: actually-set-intmap ( addr key intmap -- success )
  2dup get-index dup -1 <> if
    dup 2 pick is-found-index if
      over get-entry rot drop dup 2 pick do-finalize
      intmap-header-size + rot swap rot intmap-value-size @ move true
    else
      1 2 pick intmap-entry-count +!
      over get-entry rot 1 + over intmap-entry-key !
      intmap-header-size + rot swap rot intmap-value-size @ move true
    then
  else
    2drop 2drop false
  then ;

\ Intmap internal exception
: x-intmap-internal ( -- ) space ." intmap internal exception" cr ;

\ Intmap allocation failed exception
: x-intmap-allocate-failed ( -- ) space ." this should not be seen" cr ;

\ Expand a heap-allocated intmap
: expand-intmap ( intmap -- )
  dup intmap-count @ 2 * over intmap-entry-size *
  allocate averts x-intmap-allocate-failed
  here intmap-size allot
  0 over intmap-flags !
  2 pick intmap-value-size @ over intmap-value-size !
  2 pick intmap-count @ 2 * over intmap-count !
  0 over intmap-entry-count !
  0 over intmap-finalizer !
  0 over intmap-finalizer-arg !
  2dup intmap-entries !
  dup intmap-entries @ over intmap-count @ 2 pick intmap-entry-size *
  0 fill
  2 pick intmap-count @ 0 ?do
    i 3 pick get-entry dup intmap-entry-key @ 0 <> if
      dup intmap-header-size + swap intmap-entry-key @ 1 -
      2 pick actually-set-intmap averts x-intmap-internal
    else
      drop
    then
  loop
  drop intmap-size negate allot
  over intmap-entries @ free!
  over intmap-count @ 2 * 2 pick intmap-count !
  swap intmap-entries ! ;

intmap-wordlist set-current

\ Zero intmap size specified
: x-zero-intmap-size ( -- ) space ." zero not valid intmap size" cr ;

\ Zero value size specified
: x-zero-value-size ( -- ) space ." zero not valid value size" cr ;

\ Allot an intmap
: allot-intmap ( value-size max-entry-count -- intmap )
  dup 0 = if 2drop ['] x-zero-intmap-size ?raise then
  over 0 = if 2drop ['] x-zero-value-size ?raise then
  align here intmap-size allot
  rot over intmap-value-size !
  0 over intmap-finalizer-arg !
  0 over intmap-finalizer !
  2dup intmap-count !
  0 over intmap-entry-count !
  0 over intmap-flags !
  align here 2 pick 2 pick intmap-entry-size * allot
  over intmap-entries !
  dup intmap-entries @ rot 2 pick intmap-entry-size * 0 fill ;

\ Allocate an intmap
: allocate-intmap ( value-size initial-entry-count -- intmap )
  dup 0 = if 2drop ['] x-zero-intmap-size ?raise then
  over 0 = if 2drop ['] x-zero-value-size ?raise then
  intmap-size allocate!
  rot over intmap-value-size !
  0 over intmap-finalizer-arg !
  0 over intmap-finalizer !
  2dup intmap-count !
  0 over intmap-entry-count !
  intmap-allocated over intmap-flags !
  2dup intmap-entry-size * allocate!
  over intmap-entries !
  dup intmap-entries @ rot 2 pick intmap-entry-size * 0 fill ;

\ Set a finalizer
: set-intmap-finalizer ( finalizer finalizer-arg intmap -- )
  tuck intmap-finalizer-arg ! intmap-finalizer ! ;

\ Clear an intmap
: clear-intmap ( intmap -- )
  dup intmap-count @ 0 ?do
    i over is-found-index if
      i over get-entry dup 2 pick do-finalize 0 swap !
    then
  loop
  0 swap intmap-entry-count ! ;

\ Intmap is not allocated exception
: x-intmap-not-allocated ( -- ) space ." intmap is not allocated" cr ;

\ Destroy an allocated intmap
: destroy-intmap ( intmap -- )
  dup intmap-flags @ intmap-allocated and 0 = if
    drop ['] x-intmap-not-allocated ?raise
  then
  dup clear-intmap dup intmap-entries @ free! free! ;

\ Evaluate an xt for each member of an intmap; note that the internal state
\ is hidden from the xt, so the xt can transparently access the outside stack
: iter-intmap ( xt intmap -- )
  0 begin
    2dup swap intmap-count @ < if
      2dup swap is-found-index if
	2dup swap get-entry swap >r swap >r swap >r
	dup intmap-header-size + swap @ 1 - r@ execute
	r> r> r>
      then
      1 + false
    else
      drop 2drop true
    then
  until ;

\ Get a value from an intmap
: get-intmap ( addr key intmap -- found )
  tuck get-index dup -1 <> if
    dup 2 pick is-found-index if
      over get-entry intmap-header-size +
      rot rot intmap-value-size @ move true
    else
      drop 2drop false
    then
  else
    drop 2drop false
  then ;

\ Set a value in an intmap
: set-intmap ( addr key intmap -- success )
  dup intmap-flags @ intmap-allocated and if
    dup intmap-entry-count @ over intmap-count @ 2 / > if
      dup ['] expand-intmap try dup ['] x-intmap-allocate-failed = if
	drop 0
      then
      ?raise
    then
  then
  actually-set-intmap ;

\ Delete a value in an intmap
: delete-intmap ( key intmap -- found )
  tuck get-index dup -1 <> if
    dup 2 pick is-found-index if
      -1 2 pick intmap-entry-count +!
      over get-entry dup rot do-finalize 0 swap intmap-entry-key ! true
    else
      2drop false
    then
  else
    2drop false
  then ;

\ Get whether a key is a member of an intmap
: member-intmap ( key intmap -- found )
  tuck get-index dup -1 <> if
    swap is-found-index
  else
    2drop false
  then ;

\ Intmap does not contain cells exception
: x-non-cell-intmap ( -- ) space ." non-cell intmap" cr ;

\ Intmap does not contain double cells exception
: x-non-2cell-intmap ( -- ) space ." non-double cell intmap" cr ;

\ Get a cell from an intmap
: get-intmap-cell ( key intmap -- value found )
  dup intmap-value-size @ 1 cells = averts x-non-cell-intmap
  here dup >r 1 cells allot rot rot get-intmap -1 cells allot r> @ swap if
    true
  else
    drop 0 false
  then ;

\ Get a double cell from an intmap
: get-intmap-2cell ( key intmap -- value1 value2 found )
  dup intmap-value-size @ 2 cells = averts x-non-2cell-intmap
  here dup >r 2 cells allot rot rot get-intmap -2 cells allot r> 2@ rot if
    true
  else
    2drop 0 0 false
  then ;

\ Set a cell in an intmap
: set-intmap-cell ( value key intmap -- success )
  dup intmap-value-size @ 1 cells = averts x-non-cell-intmap
  rot here tuck ! 1 cells allot rot rot set-intmap -1 cells allot ;

\ Set a double cell in an intmap
: set-intmap-2cell ( value1 value2 key intmap -- success )
  dup intmap-value-size @ 2 cells = averts x-non-2cell-intmap
  >r >r here rot rot 2 pick 2! 2 cells allot r> r> set-intmap -2 cells allot ;

\ Intmaps do not share a value size
: x-value-sizes-not-matching ( -- )
  space ." intmap value sizes do not match" cr ;

\ Unable to copy intmap
: x-unable-to-copy-intmap ( -- ) space ." unable to copy intmap" cr ;

\ Copy one intmap into another intmap
: copy-intmap ( source-intmap dest-intmap -- )
  over intmap-value-size @ over intmap-value-size @ =
  averts x-value-sizes-not-matching
  [: 2 pick set-intmap averts x-unable-to-copy-intmap ;] rot iter-intmap drop ;

base ! set-current set-order
