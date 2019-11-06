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

\ Ultimate superclass
create object cell default-class-method-count allocate-intmap , 0 , 0 ,

\ Begin defining a class's members
: begin-class ( superclass -- )
  create cell default-class-method-count allocate-intmap , ,
  here 0 0 , ;

\ End defining a class's members
: end-class ( -- ) swap ! ;

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

\ Undefined method exception
: x-undefined-method ( -- ) space ." undefined method" cr ;

\ General OOP exception
: x-oop-failure ( -- ) space ." oop failure" cr ;

\ Invoke undefined method handler
: invoke-undefined-method ( method-index data class -- )
  &undefined-method over @ get-intmap-cell if
    execute
  else
    drop begin
      super dup 0 <> if
	&undefined-method over @ get-intmap-cell if
	  execute exit
	else
	  drop
	then
      else
	['] x-undefined-method ?raise
      then
    again
  then ;

\ Invoke a method
: invoke-method ( data class method-index -- )
  dup 2 pick @ get-intmap-cell if
    swap drop execute
  else
    drop rot rot 2dup 2>r rot begin
      rot rot super dup 0 <> if
	rot dup 2 pick @ get-intmap-cell if
	  swap drop r> drop r> drop execute exit
	else
	  drop
	then
      else
	2drop 2r> invoke-undefined-method exit
      then
    again
  then ;

\ Declare a method with a specific index
: method-with-index ( index "method" -- ) create , does> @ invoke-method ;

\ Declare a method
: method ( "name" -- )
  next-method-index @ dup 1 + next-method-index ! method-with-index ;

\ Declare an undefined method
&undefined-method method-with-index undefined-method

\ Get method index
: method-index ( xt -- index ) >body @ ;

\ Define a method
: :method ( class "method" -- )
  ' method-index :noname swap rot @ set-intmap-cell averts x-oop-failure ;

base ! set-current set-order
