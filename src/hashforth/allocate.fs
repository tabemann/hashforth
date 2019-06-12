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

wordlist constant allocate-wordlist
forth-wordlist lambda-wordlist allocate-wordlist 3 set-order
allocate-wordlist set-current

wordlist constant allocate-private-wordlist
forth-wordlist lambda-wordlist allocate-wordlist allocate-private-wordlist
4 set-order
allocate-private-wordlist set-current

\ The block header structure
begin-structure block-header-size
  \ The block flags (currently just IN-USE)
  field: block-flags

  \ The size of the preceding block in memory, excluding the header, in
  \ multiples of 32 bytes
  wfield: prev-block-size

  \ The size of this block in memory, excluding the header, in multiples of
  \ 32 bytes
  wfield: block-size

  \ The preceding free block in its sized free list
  field: prev-sized-block

  \ The succeeding free block in its sized free list
  field: next-sized-block
end-structure

\ The heap structure
begin-structure heap-size
  \ The largest size in the array of sized free lists in multiples of 32 bytes
  field: high-block-size

  \ The log2 of the largest size in the array of sized free lists in multiples
  \ of 32 bytes
  field: high-block-size-log2
  
  \ The array of sized free lists
  field: sized-blocks
end-structure

\ Debugging code for block size
: block-size ( addr -- addr )
  ( [: ." block-size: " dup block-size w@ . ;] as-non-task-io ) block-size ;

\ Block is in use flag
1 constant in-use

\ Convert a size in bytes to a size in multiples of 32 bytes
: >size ( bytes -- 32-bytes )
  dup 32 umod 0 > if 5 rshift 1 + else 5 rshift then ;

\ Convert a size in multiples of 32 bytes to a size in bytes
: size> ( 32-bytes -- bytes ) 5 lshift ;

\ The block header size as a multiple of 32 bytes
block-header-size >size constant block-header-size-32

\ Get the log2 of a value
: log2 ( u -- u ) 0 begin over 1 > while 1 + swap 1 rshift swap repeat nip ;

\ Get the pow2 of a value
: pow2 ( u -- u ) 1 swap lshift ;

\ Get the log2 with ceiling of a value
: log2-ceiling ( u -- u ) dup dup log2 pow2 - 0 > if log2 1 + else log2 then ;

\ Get the pow2 of the log2 with ceiling of a value
: ceiling2 ( u -- u ) log2-ceiling pow2 ;

\ Get the index into the array of sized free lists for a block size
: >index ( 32-bytes heap -- index )
  dup high-block-size @ 2 pick ceiling2 < if
    nip high-block-size-log2 @
  else
    drop log2-ceiling
  then ( [: ." >index: " dup . ;] as-non-task-io ) ;

\ Get the index into the array of sized free lists for a block size
: >index-fill ( 32-bytes heap -- index )
  dup high-block-size @ 2 pick < if
    nip high-block-size-log2 @
  else
    drop log2
  then ( [: ." >index-fill: " dup . ;] as-non-task-io ) ;

\ Get the header of a block
: block-header ( addr -- block ) block-header-size - ;

\ Get the data of a block
: block-data ( block -- addr ) block-header-size + ;

\ Get the previous block
: prev-block ( block -- block )
  dup prev-block-size w@ 0 > if
    dup prev-block-size w@ size> - block-header-size -
  else
    drop 0
  then
  ( [: ." prev-block: " dup . ;] as-non-task-io ) ;

\ Get the next block
: next-block ( block -- block )
  dup block-size w@ size> + block-header-size +
  ( [: ." next-block: " dup . ;] as-non-task-io ) ;

\ Remove a block from its sized block list
: unlink-block ( block heap -- )
  ( [:
    ." unlinking block: " over .
    over prev-sized-block @ .
    over next-sized-block @ .
  ;] as-non-task-io )
  swap dup next-sized-block @ if
    dup prev-sized-block @ over next-sized-block @ prev-sized-block !
  then
  dup prev-sized-block @ if
    dup next-sized-block @ swap prev-sized-block @ next-sized-block ! drop
  else
    dup block-size w@ 2 pick >index-fill cells rot sized-blocks @ +
    swap next-sized-block @ swap !
  then ;

\ Add a block to its sized block list
: link-block ( block heap -- )
  ( [: ." linking block: " over . ;] as-non-task-io )
  over 0 swap prev-sized-block !
  over block-size w@ over >index-fill cells swap sized-blocks @ +
  dup @ if
    2dup @ prev-sized-block !
    2dup @ swap next-sized-block !
  else
    over 0 swap next-sized-block !
  then
  ( [: over . ;] as-non-task-io )
  ! ;

\ Update the succeeding block previous block size
: update-prev-block-size ( block -- )
  dup block-size w@ dup size> rot block-data + prev-block-size
  ( [: over ." update-prev-block-size: " . ;] as-non-task-io ) w! ;

\ Merge with a preceding block
: merge-prev-block ( block heap -- block )
  swap dup prev-block ?dup if
    block-flags @ in-use and 0 = if
      ( [: ." merging prev block: " dup . ;] as-non-task-io )
      dup prev-block rot unlink-block
      dup block-size w@ block-header-size-32 +
      over prev-block block-size w@ + over prev-block block-size w!
      prev-block dup update-prev-block-size
    else
      nip
    then
  else
    nip
  then ;

\ Merge with a succeeding block
: merge-next-block ( block heap -- )
  swap dup next-block block-flags @ in-use and 0 = if
    ( [: ." merging next block: " dup . ;] as-non-task-io )
    dup next-block rot unlink-block
    dup dup next-block block-size w@ block-header-size-32 +
    over block-size w@ + swap block-size w!
    update-prev-block-size
  else
    2drop
  then ;

\ Find a suitable index for a new block
: find-index ( 32-bytes heap -- index )
  tuck >index begin
    dup 2 pick high-block-size-log2 @ <= if
      dup cells 2 pick sized-blocks @ + @ if
	nip true
      else
	1 + false
      then
    else
      2drop -1 true
    then
  until ;

\ Split a block
: split-block ( 32-bytes block heap -- )
  ( [: ." split block: " 2 pick . over . ;] as-non-task-io )
  2dup unlink-block
  over block-data 3 pick size> +
  3 pick over prev-block-size w! ( [: dup prev-block-size w@ . ;] as-non-task-io )
  2 pick block-size w@ 4 pick block-header-size-32 + - over block-size w!
  ( [: dup block-size w@ . ;] as-non-task-io )
  dup update-prev-block-size
  0 over block-flags !
  swap link-block
  block-size w! ;

\ Search for a block that fits, or return 0 if none fit
: search-blocks ( 32-bytes block -- block )
  begin
    dup 0 <> if
      2dup block-size w@ <= if nip true else next-sized-block @ false then
    else
      nip true
    then
  until ;

\ Allocate a block
: allocate-block ( allocate-size heap -- block )
  swap >size dup 2 pick find-index dup -1 <> if
    cells 2 pick sized-blocks @ + @ over swap search-blocks ?dup if
      ( [: ." allocating block: " over . dup . ;] as-non-task-io )
      dup block-size w@ ( heap allocate-size block block-size )
      ( [: .s ;] as-non-task-io )
      2 pick block-header-size-32 2 * + >= if
	2dup 4 pick split-block
      else
	dup 3 pick unlink-block
      then
      dup block-flags @ in-use or over block-flags !
      nip nip
    else
      2drop 0
    then
  else
    2drop drop 0
  then ( [: ." prev-block-size: " dup prev-block-size w@ . ;] as-non-task-io ) ;

\ Free a block
: free-block ( block heap -- )
  ( [: ." freeing a block: " over . ;] as-non-task-io )
  tuck merge-prev-block
  2dup swap merge-next-block
  dup block-flags @ in-use not and over block-flags !
  swap link-block ;

\ Resize a block by allocating a new block, copying data to it, and then freeing
\ the original block
: allocate-resize-block ( 32-bytes block heap -- new-block )
  2 pick size> over allocate-block ?dup if
    2 pick block-data over block-data 4 pick block-size w@ size> move
    rot rot free-block nip
  else
    2drop drop 0
  then ;

\ Resize a block
: resize-block ( allocate-size block heap -- )
  rot >size rot dup block-size w@ block-header-size-32 2 * - 2 pick
  ( [: ." sizes: " over . dup . ;] as-non-task-io )
  >= if
    dup >r rot split-block r> ( [: ." case0 " ;] as-non-task-io )
  else
    dup block-size w@ 2 pick < if
      dup next-block block-flags @ in-use and 0 = if
	dup next-block block-size w@ block-header-size-32 +
	over block-size w@ + 2 pick >= if
	  dup 3 pick merge-next-block
	  dup block-size w@ 2 pick block-header-size-32 2 * + >= if
	    dup >r rot split-block r> ( [: ." case1 " ;] as-non-task-io )
	  else
	    nip nip ( [: ." case2 " ;] as-non-task-io )
	  then
	else
	  rot allocate-resize-block ( [: ." case3 " ;] as-non-task-io )
	then
      else
	rot allocate-resize-block ( [: ." case4 " ;] as-non-task-io )
      then
    else
      nip nip ( [: ." case5 " ;] as-non-task-io )
    then
  then ;

\ Create an initial block
: init-block ( heap-size -- block )
  >size ceiling2 align here over size> block-header-size 2 * + allot
  tuck block-size w!
  0 over prev-block-size w!
  0 over block-flags !
  0 over prev-sized-block !
  0 over next-sized-block !
  dup dup block-size w@ size> swap block-header-size + +
  0 over block-size w!
  over block-size w@ over prev-block-size w!
  in-use over block-flags !
  0 over prev-sized-block !
  0 swap next-sized-block ! ;

allocate-wordlist set-current

\ Create an heap with a specified heap size and a specified largest size
\ in the array of sized free lists
: init-heap ( high-block-size -- heap )
  align here heap-size allot
  swap >size ceiling2 over high-block-size !
  dup high-block-size @ log2 over high-block-size-log2 !
  align here over high-block-size-log2 @ 1 + cells allot
  over sized-blocks !
  dup sized-blocks @ over high-block-size-log2 @ cells 0 fill ;

\ Initialize the heap
1024 1024 * init-heap constant shared-heap

\ The private heap user variable
user private-heap 0 private-heap !

\ Get the current heap
: get-heap ( -- heap ) private-heap @ ?dup if else shared-heap then ;

\ Expand the heap
: expand-heap ( block-size -- )
  get-heap over init-block
  rot >size 2 pick >index-fill cells rot sized-blocks @ + ! ;

\ Initialize a private heap
: init-private-heap ( high-block-size -- )
  private-heap @ 0 = if
    init-heap private-heap !
  else
    drop
  then ;

\ Add memory to the heap
1024 1024 * 8 * expand-heap

\ Allocate memory on a heap; returns -1 on success and 0 on failure
: allocate-with-heap ( bytes heap -- addr -1|0 )
  swap ?dup if
    swap allocate-block ?dup if block-data true else 0 false then
  else
    drop 0 true
  then ;

\ Free memory on a heap; returns -1 on success and 0 on failure
: free-with-heap ( addr heap -- -1|0 )
  swap ?dup if block-header swap free-block true else drop true then ;

\ Resize memory on a heap; returns -1 on success and 0 on failure
: resize-with-heap ( addr new-bytes heap -- addr -1|0 )
  >r swap ?dup if
    over if
      block-header r@ resize-block ?dup if
	block-data true
      else
	0 false
      then
    else
      nip r@ free-with-heap 0 swap
    then
  else
    r@ allocate-with-heap
  then
  r> drop ;

\ Switch over to FORTH-WORDLIST for public-facing words
forth-wordlist set-current

\ Allocate memory on the current heap; returns -1 on success and 0 on failure
: allocate ( bytes -- addr -1|0 ) get-heap allocate-with-heap ;

\ Free memory on the current heap; returns -1 on success and 0 on failure
: free ( addr -- -1|0 ) get-heap free-with-heap ;

\ Resize memory on the current heap; returns -1 on success and 0 on failure
: resize ( addr new-bytes -- addr -1|0 ) get-heap resize-with-heap ;

\ Memory management failure exception
: x-memory-management-failure ( -- )
  space ." failed to allocate/free memory" cr ;

\ Allocate memory in the heap, raising an exception if allocation fails.
: allocate! ( bytes -- addr ) allocate averts x-memory-management-failure ;

\ Resize memory in the heap, raising an exception if allocation fails.
: resize! ( addr new-bytes -- new-addr )
  resize averts x-memory-management-failure ;

\ Free memory in the heap, raising an exception if freeing fails.
: free! ( addr -- ) free averts x-memory-management-failure ;

base ! set-current set-order