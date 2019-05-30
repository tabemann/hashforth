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

GET-ORDER GET-CURRENT BASE @

DECIMAL
FORTH-WORDLIST 1 SET-ORDER
FORTH-WORDLIST SET-CURRENT

WORDLIST CONSTANT ALLOCATE-WORDLIST
FORTH-WORDLIST ALLOCATE-WORDLIST 2 SET-ORDER
ALLOCATE-WORDLIST SET-CURRENT

\ The block header structure
BEGIN-STRUCTURE BLOCK-HEADER-SIZE
  \ The block flags (currently just IN-USE)
  FIELD: BLOCK-FLAGS

  \ The size of the preceding block in memory, excluding the header, in
  \ multiples of 32 bytes
  WFIELD: PREV-BLOCK-SIZE

  \ The size of this block in memory, excluding the header, in multiples of
  \ 32 bytes
  WFIELD: BLOCK-SIZE

  \ The preceding free block in its sized free list
  FIELD: PREV-SIZED-BLOCK

  \ The succeeding free block in its sized free list
  FIELD: NEXT-SIZED-BLOCK
END-STRUCTURE

\ The allocator structure
BEGIN-STRUCTURE ALLOCATOR-SIZE
  \ The largest size in the array of sized free lists in multiples of 32 bytes
  FIELD: HIGH-BLOCK-SIZE

  \ The log2 of the largest size in the array of sized free lists in multiples
  \ of 32 bytes
  FIELD: HIGH-BLOCK-SIZE-LOG2
  
  \ The array of sized free lists
  FIELD: SIZED-BLOCKS
END-STRUCTURE

\ Block is in use flag
1 CONSTANT IN-USE

\ Convert a size in bytes to a size in multiples of 32 bytes
: >SIZE ( bytes -- 32-bytes )
  DUP 32 UMOD 0 > IF 5 RSHIFT 1 + ELSE 5 RSHIFT THEN ;

\ Convert a size in multiples of 32 bytes to a size in bytes
: SIZE> ( 32-bytes -- bytes ) 5 LSHIFT ;

\ The block header size as a multiple of 32 bytes
BLOCK-HEADER-SIZE >SIZE CONSTANT BLOCK-HEADER-SIZE-32

\ Get the log2 of a value
: LOG2 ( u -- u ) 0 BEGIN OVER 1 > WHILE 1 + SWAP 1 RSHIFT SWAP REPEAT NIP ;

\ Get the pow2 of a value
: POW2 ( u -- u ) 1 SWAP LSHIFT ;

\ Get the log2 with ceiling of a value
: LOG2-CEILING ( u -- u ) DUP DUP LOG2 POW2 - 0 > IF LOG2 1 + ELSE LOG2 THEN ;

\ Get the pow2 of the log2 with ceiling of a value
: CEILING2 ( u -- u ) LOG2-CEILING POW2 ;

\ Get the index into the array of sized free lists for a block size
: >INDEX ( 32-bytes allocator -- index )
  DUP HIGH-BLOCK-SIZE @ 2 PICK CEILING2 < IF
    NIP HIGH-BLOCK-SIZE-LOG2 @
  ELSE
    DROP LOG2-CEILING
  THEN ;

\ Get the index into the array of sized free lists for a block size
: >INDEX-FILL ( 32-bytes allocator -- index )
  DUP HIGH-BLOCK-SIZE @ 2 PICK < IF
    NIP HIGH-BLOCK-SIZE-LOG2 @
  ELSE
    DROP LOG2
  THEN ;

\ Get the header of a block
: BLOCK-HEADER ( addr -- block ) BLOCK-HEADER-SIZE - ;

\ Get the data of a block
: BLOCK-DATA ( block -- addr ) BLOCK-HEADER-SIZE + ;

\ Get the previous block
: PREV-BLOCK ( block -- block )
  DUP PREV-BLOCK-SIZE @ 0 > IF
    DUP PREV-BLOCK-SIZE @ SIZE> - BLOCK-HEADER-SIZE -
  ELSE
    0
  THEN ;

\ Get the next block
: NEXT-BLOCK ( block -- block )
  DUP DUP BLOCK-SIZE @ SIZE> + BLOCK-HEADER-SIZE + ;

\ Remove a block from its sized block list
: UNLINK-BLOCK ( block allocator -- )
  SWAP DUP NEXT-SIZED-BLOCK @ IF
    DUP PREV-SIZED-BLOCK @ OVER NEXT-SIZED-BLOCK @ PREV-SIZED-BLOCK !
  THEN
  DUP PREV-SIZED-BLOCK @ IF
    DUP NEXT-SIZED-BLOCK @ SWAP PREV-SIZED-BLOCK @ NEXT-SIZED-BLOCK ! DROP
  ELSE
    DUP BLOCK-SIZE W@ 2 PICK >INDEX-FILL CELLS ROT SIZED-BLOCKS @ +
    SWAP NEXT-SIZED-BLOCK @ SWAP !
  THEN ;

\ Add a block to its sized block list
: LINK-BLOCK ( block allocator -- )
  OVER 0 SWAP PREV-SIZED-BLOCK !
  OVER BLOCK-SIZE W@ OVER >INDEX-FILL CELLS SWAP SIZED-BLOCKS @ +
  DUP @ IF
    2DUP @ PREV-SIZED-BLOCK !
    2DUP @ SWAP NEXT-SIZED-BLOCK !
  ELSE
    OVER 0 SWAP NEXT-SIZED-BLOCK !
  THEN
  ! ;

\ Update the succeeding block previous block size
: UPDATE-PREV-BLOCK-SIZE ( block -- )
  DUP BLOCK-SIZE W@ DUP SIZE> ROT BLOCK-DATA + PREV-BLOCK-SIZE W! ;

\ Merge with a preceding block
: MERGE-PREV-BLOCK ( block allocator -- block )
  SWAP DUP PREV-BLOCK ?DUP IF
    BLOCK-FLAGS @ IN-USE AND 0 = IF
      DUP PREV-BLOCK ROT UNLINK-BLOCK
      DUP BLOCK-SIZE W@ BLOCK-HEADER-SIZE-32 + OVER PREV-BLOCK BLOCK-SIZE W!
      PREV-BLOCK DUP UPDATE-PREV-BLOCK-SIZE
    ELSE
      2DROP
    THEN
  ELSE
    2DROP
  THEN ;

\ Merge with a succeeding block
: MERGE-NEXT-BLOCK ( block allocator -- )
  SWAP DUP NEXT-BLOCK BLOCK-FLAGS @ IN-USE AND 0 = IF
    DUP NEXT-BLOCK ROT UNLINK-BLOCK
    DUP DUP NEXT-BLOCK BLOCK-SIZE W@ BLOCK-HEADER-SIZE-32 +
    SWAP BLOCK-SIZE W!
    UPDATE-PREV-BLOCK-SIZE
  ELSE
    2DROP
  THEN ;

\ Find a suitable index for a new block
: FIND-INDEX ( 32-bytes allocator -- index )
  TUCK >INDEX BEGIN
    DUP 2 PICK HIGH-BLOCK-SIZE-LOG2 @ <= IF
      DUP CELLS 2 PICK SIZED-BLOCKS @ + @ IF
	NIP TRUE
      ELSE
	1 + FALSE
      THEN
    ELSE
      2DROP -1 TRUE
    THEN
  UNTIL ;

\ Split a block
: SPLIT-BLOCK ( 32-bytes block allocator -- )
  OVER BLOCK-DATA 3 PICK SIZE> +
  3 PICK OVER PREV-BLOCK-SIZE W!
  2 PICK BLOCK-SIZE W@ 4 PICK BLOCK-HEADER-SIZE-32 + - OVER BLOCK-SIZE W!
  0 OVER BLOCK-FLAGS !
  SWAP LINK-BLOCK
  BLOCK-SIZE W! ;

\ Allocate a block
: ALLOCATE-BLOCK ( allocate-size allocator -- block )
  SWAP >SIZE DUP 2 PICK FIND-INDEX DUP -1 <> IF
    CELLS 2 PICK SIZED-BLOCKS @ + @
    DUP BLOCK-SIZE W@ ( allocator allocate-size block block-size )
    2 PICK BLOCK-HEADER-SIZE-32 2 * + >= IF
      2DUP 4 PICK SPLIT-BLOCK
    THEN
    DUP BLOCK-FLAGS @ IN-USE OR OVER BLOCK-FLAGS !
    NIP TUCK SWAP UNLINK-BLOCK
  ELSE
    2DROP DROP 0
  THEN ;

\ Free a block
: FREE-BLOCK ( block allocator -- )
  true set-trace TUCK MERGE-PREV-BLOCK 2DUP SWAP MERGE-NEXT-BLOCK SWAP LINK-BLOCK false set-trace ;

\ Resize a block by allocating a new block, copying data to it, and then freeing
\ the original block
: ALLOCATE-RESIZE-BLOCK ( 32-bytes block allocator -- new-block )
  2 PICK SIZE> OVER ALLOCATE-BLOCK ?DUP IF
    2 PICK BLOCK-DATA OVER BLOCK-DATA 3 PICK BLOCK-SIZE W@ SIZE> MOVE
    ROT ROT FREE-BLOCK NIP
  ELSE
    2DROP DROP 0
  THEN ;

\ Resize a block
: RESIZE-BLOCK ( allocate-size block allocator -- )
  ROT >SIZE ROT DUP BLOCK-SIZE W@ BLOCK-HEADER-SIZE-32 2 * - 2 PICK >= IF
    DUP >R ROT SPLIT-BLOCK R>
  ELSE
    DUP BLOCK-SIZE W@ 2 PICK > IF
      DUP NEXT-BLOCK BLOCK-FLAGS @ IN-USE AND 0 = IF
	DUP NEXT-BLOCK BLOCK-SIZE W@ BLOCK-HEADER-SIZE-32 +
	OVER BLOCK-SIZE W@ + 2 PICK >= IF
	  DUP 3 PICK MERGE-NEXT-BLOCK
	  DUP BLOCK-SIZE W@ 2 PICK BLOCK-HEADER-SIZE-32 2 * + <= IF
	    DUP >R ROT SPLIT-BLOCK R>
	  ELSE
	    NIP NIP
	  THEN
	ELSE
	  ROT ALLOCATE-RESIZE-BLOCK
	THEN
      ELSE
	ROT ALLOCATE-RESIZE-BLOCK
      THEN
    ELSE
      NIP NIP
    THEN
  THEN ;

\ Create an initial block
: INIT-BLOCK ( heap-size -- block )
  >SIZE CEILING2 ALIGN HERE OVER SIZE> BLOCK-HEADER-SIZE 2 * + ALLOT
  TUCK BLOCK-SIZE W!
  0 OVER PREV-BLOCK-SIZE W!
  0 OVER BLOCK-FLAGS !
  0 OVER PREV-SIZED-BLOCK !
  0 OVER NEXT-SIZED-BLOCK !
  DUP DUP BLOCK-SIZE W@ SIZE> SWAP BLOCK-HEADER-SIZE + +
  0 OVER BLOCK-SIZE W!
  OVER BLOCK-SIZE W@ OVER PREV-BLOCK-SIZE W!
  IN-USE OVER BLOCK-FLAGS !
  0 OVER PREV-SIZED-BLOCK !
  0 SWAP NEXT-SIZED-BLOCK ! ;

\ Create an allocator with a specified heap size and a specified largest size
\ in the array of sized free lists
: INIT-ALLOCATOR ( heap-size high-block-size -- allocator )
  ALIGN HERE ALLOCATOR-SIZE ALLOT
  SWAP >SIZE CEILING2 OVER HIGH-BLOCK-SIZE !
  DUP HIGH-BLOCK-SIZE @ LOG2 OVER HIGH-BLOCK-SIZE-LOG2 !
  ALIGN HERE OVER HIGH-BLOCK-SIZE-LOG2 @ CELLS ALLOT
  OVER SIZED-BLOCKS !
  DUP SIZED-BLOCKS @ OVER HIGH-BLOCK-SIZE-LOG2 @ CELLS 0 FILL
  OVER INIT-BLOCK
  ROT >SIZE 2 PICK >INDEX-FILL CELLS 2 PICK SIZED-BLOCKS @ + ! ;

\ Initialize the allocator
65536 4096 INIT-ALLOCATOR CONSTANT MY-ALLOCATOR

\ Switch over to FORTH-WORDLIST for public-facing words
FORTH-WORDLIST SET-CURRENT

\ My allocate word
: MY-ALLOCATE ( bytes -- addr -1|0 )
  ?DUP IF
    MY-ALLOCATOR ALLOCATE-BLOCK ?DUP IF BLOCK-DATA TRUE ELSE 0 FALSE THEN
  ELSE
    0 TRUE
  THEN ;

\ My free word
: MY-FREE ( addr -- -1|0 )
  ?DUP IF BLOCK-HEADER MY-ALLOCATOR FREE-BLOCK TRUE ELSE TRUE THEN ;

\ My resize word
: MY-RESIZE ( bytes addr -- addr -1|0 )
  ?DUP IF
    OVER IF
      BLOCK-HEADER MY-ALLOCATOR RESIZE-BLOCK ?DUP IF
	BLOCK-DATA TRUE
      ELSE
	0 FALSE
      THEN
    ELSE
      NIP MY-FREE 0 SWAP
    THEN
  ELSE
    MY-ALLOCATE
  THEN ;

BASE ! SET-CURRENT SET-ORDER