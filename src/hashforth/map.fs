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

wordlist constant map-wordlist
forth-wordlist lambda-wordlist map-wordlist 3 set-order
map-wordlist set-current

wordlist constant map-private-wordlist
forth-wordlist lambda-wordlist map-private-wordlist map-wordlist
lambda-wordlist 5 set-order
map-private-wordlist set-current

\ The map entry structure
begin-structure map-header-size
  \ The map entry key size
  field: map-entry-key-size

  \ The map entry value size
  field: map-entry-value-size
end-structure

\ The map structure
begin-structure map-size
  \ The number of map entry fields, regardless of whether they are used
  field: map-count

  \ The number of actual map entries
  field: map-entry-count

  \ The map entries
  field: map-entries

  \ The finalizer word
  field: map-finalizer

  \ The extra finalizer argument
  field: map-finalizer-arg
end-structure

\ Get the map entry size
: map-entry-size ( entry -- )
  dup map-entry-key-size @ swap map-entry-value-size @ +
  map-header-size + ;

\ Calculate a hash for a key
: hash-key ( addr bytes -- hash )
  0 begin over 0 > while
    dup 7 lshift swap [ 1 cells 8 * 7 - ] literal rshift or
    2 pick c@ xor rot 1 + rot 1 - rot
  repeat
  nip nip ;

\ Convert a key into an entry key
: get-entry-key ( addr bytes map -- entry-key )
  rot rot hash-key swap map-count @ umod ;

\ Get entry address
: get-entry ( entry-key map -- entry ) map-entries @ swap cells + ;

\ Compare keys
: compare-keys ( addr bytes entry -- match )
  dup map-header-size + swap map-entry-key-size @ equal-strings? ;

\ Get the index for a key; return -1 if key is not found
: get-index ( addr bytes map -- index )
  2 pick 2 pick 2 pick get-entry-key dup >r begin
    dup 2 pick get-entry
    dup @ 0 = if ( addr bytes map entry-key entry )
      drop nip nip nip r> drop true
    else
      4 pick 4 pick rot @ compare-keys if
	nip nip nip r> drop true
      else
	1 + over map-count @ umod dup r@ <> if
	  false
	else
	  2drop 2drop r> drop -1 true
	then
      then
    then
  until ;

\ Get whether an index is a found index
: is-found-index ( index map -- ) get-entry @ 0 <> ;

\ Get the key of an entry
: entry-key ( entry -- addr bytes )
  dup map-header-size + swap map-entry-key-size @ ;

\ Get the value of an entry
: entry-value ( entry -- addr bytes )
  dup map-header-size + over map-entry-key-size @ +
  swap map-entry-value-size @ ;

\ Carry out finalizing of an entry if there is a finalizer xt
: do-finalize ( entry map -- )
  dup >r map-finalizer @ if
    dup entry-value rot entry-key r@ map-finalizer-arg @
    r> map-finalizer @ execute
  else
    drop r> drop
  then ;

\ Actually set a value in a map
: actually-set-map ( addr map -- success )
  over entry-key 2 pick get-index dup -1 <> if
    dup 2 pick is-found-index if
      2dup swap get-entry @ 2 pick do-finalize
      swap get-entry dup @ free! ! true
    else
      1 2 pick map-entry-count +!
      swap get-entry ! true
    then
  else
    drop 2drop false
  then ;

\ Allocate an entry block
: allocate-entry ( value-addr value-bytes key-addr key-bytes -- block-addr )
  2 pick over + map-header-size + allocate if
    2dup map-entry-key-size !
    3 pick over map-entry-value-size !
    rot over map-header-size + 3 roll move
    rot over map-header-size + 2 pick map-entry-key-size @ + 3 roll move
  else
    2drop 2drop 0
  then ;

\ Map internal exception
: x-map-internal ( -- ) space ." map internal exception" cr ;

\ Map allocation failed exception
: x-map-allocate-failed ( -- ) space ." this should not be seen" cr ;

\ Expand a map
: expand-map ( map -- )
  dup map-count @ 2 * cells allocate averts x-map-allocate-failed
  here map-size allot
  2 pick map-count @ 2 * over map-count !
  0 over map-entry-count !
  0 over map-finalizer !
  0 over map-finalizer-arg !
  2dup map-entries !
  dup map-entries @ over map-count @ cells 0 fill
  2 pick map-count @ 0 ?do
    i 3 pick get-entry @ ?dup if
      over actually-set-map averts x-map-internal
    then
  loop
  drop map-size negate allot
  over map-entries @ free!
  over map-count @ 2 * 2 pick map-count !
  swap map-entries ! ;

map-wordlist set-current

\ Zero map size specified
: x-zero-map-size ( -- ) space ." zero not valid map size" cr ;

\ Allocate a map
: allocate-map ( initial-entry-count -- map )
  dup 0 = if 2drop ['] x-zero-map-size ?raise then
  map-size allocate!
  2dup map-count !
  0 over map-entry-count !
  0 over map-finalizer-arg !
  0 over map-finalizer !
  over cells allocate!
  over map-entries !
  dup map-entries @ rot cells 0 fill ;

\ Set a finalizer
: set-map-finalizer ( finalizer finalizer-arg map -- )
  tuck map-finalizer-arg ! map-finalizer ! ;

\ Clear a map
: clear-map ( map -- )
  dup map-count @ 0 ?do
    i over is-found-index if
      i over get-entry dup @ dup 3 pick do-finalize free! 0 swap !
    then
  loop
  0 swap map-entry-count ! ;

\ Destroy a map
: destroy-map ( map -- )
  dup clear-map dup map-entries @ free! free! ;

\ Evaluate an xt for each member of a map; note that the internal state
\ is hidden from the xt, so the xt can transparently access the outside stack
: iter-map ( xt map -- )
  0 begin
    2dup swap map-count @ < if
      2dup swap is-found-index if
	2dup swap get-entry @ swap >r swap >r swap >r
	dup entry-value rot entry-key r@ execute
	r> r> r>
      then
      1 + false
    else
      drop 2drop true
    then
  until ;

\ Get a value from a map
: get-map ( key-addr key-bytes map -- value-addr value-bytes found )
  dup >r get-index dup -1 <> if
    dup r@ is-found-index if
      r> get-entry @ entry-value true
    else
      drop r> drop 0 0 false
    then
  else
    drop r> drop 0 0 false
  then ;

\ Set a value in a map
: set-map ( value-addr value-bytes key-addr key-bytes map -- success )
  dup map-entry-count @ over map-count @ 2 / > if
    dup ['] expand-map try dup ['] x-map-allocate-failed = if
      drop 0
    then
    ?raise
  then
  >r allocate-entry ?dup if
    dup r> actually-set-map if
      drop true
    else
      free! false
    then
  else
    r> drop false
  then ;

\ Delete a value in a map
: delete-map ( key-addr key-bytes map -- found )
  dup >r get-index dup -1 <> if
    dup r@ is-found-index if
      -1 r@ map-entry-count +!
      r@ get-entry dup @ r> do-finalize 0 swap ! true
    else
      drop r> drop false
    then
  else
    drop r> drop false
  then ;

\ Get whether a key is a member of a map
: member-map ( key-addr key-bytes intmap -- found )
  dup >r get-index dup -1 <> if
    r> is-found-index
  else
    drop r> drop false
  then ;

\ Unable to copy map
: x-unable-to-copy-map ( -- ) space ." unable to copy map" cr ;

\ Copy a map into another map
: copy-map ( source-map dest-map -- )
  [: 4 pick set-map averts x-unable-to-copy-map ;]
  rot iter-map drop ;

base ! set-current set-order
