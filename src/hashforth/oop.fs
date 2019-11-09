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

wordlist constant oop-wordlist
forth-wordlist lambda-wordlist intmap-wordlist allocate-wordlist oop-wordlist
5 set-order
oop-wordlist set-current

\ Initial method count for a class's intmap
16 constant default-class-method-count

\ Get the superclass of a class
: super ( data1 class1 -- data2 class2 )
  dup @ 0 <> if
    cell + @ ?dup if
      tuck [ 2 cells ] literal + @ - swap
    else
      0
    then
  then ;

\ Get the total amount of memory taken up by an object of a class
: class-size ( class -- bytes )
  0 begin over 0 <> while
    over [ 2 cells ] literal + @ + swap cell + @ swap
  repeat
  nip ;

\ Get the total amount of memory taken up by the superclasses of a class
: super-size ( class -- bytes )
  dup 0 <> if cell + @ class-size else drop 0 then ;

\ Allot an object in the user space
: allot-object ( class -- data class )
  dup class-size dup here over allot dup rot 0 fill over super-size + swap ;

\ Allocate an object on the current heap, returns -1 on success and 0 on failure
: allocate-object ( class -- data class -1|0 )
  dup class-size dup allocate if
    dup rot 0 fill over super-size + swap true
  else
    2drop 0 0 false
  then ;

\ Allocate an object on the current heap, raising an exception if allocation
\ fails
: allocate-object! ( class -- data class )
  dup class-size dup allocate! dup rot 0 fill over super-size + swap ;

\ Free an object, returns -1 on success and 0 on failure
: free-object ( data class -- -1|0 ) super-size - free ;

\ Free an object, raising an exception if freeing fails
: free-object! ( data class -- ) super-size - free! ;

\ The current method index
variable next-method-index 1 next-method-index !

\ An undefined method
0 constant &undefined-method

\ A global reverse-lookup map for methods
cell default-class-method-count allocate-intmap constant method-map

\ Undefined method exception
: x-undefined-method ( -- ) space ." undefined method" cr ;

\ General OOP exception
: x-oop-failure ( -- ) space ." oop failure" cr ;

\ Invoke undefined method handler
: invoke-undefined-method ( method-index data class -- )
  rot method-map get-intmap-cell averts x-oop-failure rot rot
  2dup >r >r &undefined-method over @ get-intmap-cell if
    r> r> execute
  else
    drop begin
      super dup 0 <> if
	&undefined-method over @ get-intmap-cell if
	  r> r> rot execute exit
	else
	  drop
	then
      else
	r> r> 2drop ['] x-undefined-method ?raise
      then
    again
  then ;

\ Invoke a method
: invoke-method ( data class method-index -- )
  rot rot 2dup >r >r rot dup 2 pick @ get-intmap-cell if
    swap drop r> r> rot execute
  else
    drop begin
      rot rot super dup 0 <> if
	rot dup 2 pick @ get-intmap-cell if
	  swap drop r> r> rot execute exit
	else
	  drop
	then
      else
	2drop r> r> invoke-undefined-method exit
      then
    again
  then ;

\ Declare a method with a specific index
: method-with-index ( index "method" -- )
  create dup , latestxt swap method-map set-intmap-cell averts x-oop-failure
  does> @ invoke-method ;

\ Declare a method
: method ( "name" -- )
  next-method-index @ dup 1 + next-method-index ! method-with-index ;

\ Declare a method if it does not already exist
: 'method ( "name" -- xt )
  parse-name dup 0 <> if
    2dup search-wordlists if
      rot rot 2drop
    else
      drop create-with-name next-method-index @ dup 1 + next-method-index !
      dup , latestxt swap method-map set-intmap-cell averts x-oop-failure
      latestxt does> @ invoke-method exit
    then
  else
    2drop ['] x-no-parse-found ?raise
  then ;

\ Declare an undefined method
&undefined-method method-with-index undefined-method

\ Get method index
: method-index ( xt -- index ) >body @ ;

\ Define a method
: :method ( class "method" -- )
  'method method-index :noname & rot & drop & rot & drop
  swap rot @ set-intmap-cell averts x-oop-failure ;

wordlist constant oop-field-wordlist
forth-wordlist lambda-wordlist intmap-wordlist allocate-wordlist oop-wordlist
oop-field-wordlist 6 set-order
oop-field-wordlist set-current

\ Define an arbitrarily-sized field of a class
: +field
  'method method-index create-noname swap 4 pick @ set-intmap-cell
  averts x-oop-failure over , + does> nip nip nip @ + ;

\ Define a byte-sized field of a class
: cfield:
  'method method-index create-noname swap 3 pick @ set-intmap-cell
  averts x-oop-failure dup , 1 + does> nip nip nip @ + ;

\ Define a 16-bit-sized field of a class
: hfield:
  'method method-index create-noname swap 3 pick @ set-intmap-cell
  averts x-oop-failure haligned dup , 2 + does> nip nip nip @ + ;

\ Define a 32-bit-sized field of a class
: wfield:
  'method method-index create-noname swap 3 pick @ set-intmap-cell
  averts x-oop-failure waligned dup , 4 + does> nip nip nip @ + ;

\ Define a cell-sized field of a class
: field:
  'method method-index create-noname swap 3 pick @ set-intmap-cell
  averts x-oop-failure aligned dup , cell + does> nip nip nip @ + ;

\ Define a double cell-sized field of a class
: 2field:
  'method method-index create-noname swap 3 pick @ set-intmap-cell
  averts x-oop-failure aligned dup , 2 cells + does> nip nip nip @ + ;

\ Define a method
: :method ( "method" -- )
  'method method-index :noname swap 3 pick @ set-intmap-cell
  averts x-oop-failure ;

forth-wordlist lambda-wordlist intmap-wordlist allocate-wordlist oop-wordlist
5 set-order
oop-wordlist set-current

\ Ultimate superclass
create object cell default-class-method-count allocate-intmap , 0 , 0 ,

\ Begin defining a class's members
: begin-class ( superclass -- )
  create cell default-class-method-count allocate-intmap , ,
  here 0 0 , latestxt >body swap
  get-order oop-field-wordlist swap 1 + set-order ;

\ End defining a class's members
: end-class ( -- ) nip swap ! get-order nip 1 - set-order ;

base ! set-current set-order
