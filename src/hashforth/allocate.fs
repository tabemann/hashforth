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
ALLOCATE-WORDLIST SET-ORDER

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
  
  \ The array of sized free lists
  FIELD: SIZED-BLOCKS
END-STRUCTURE

\ Block is in use flag
1 CONSTANT IN-USE

\ Convert a size in bytes to a size in multiples of 32 bytes
: >SIZE ( bytes -- 32-bytes ) 32 UMOD 0 > IF 5 RSHIFT 1 + ELSE 5 RSHIFT THEN ;

\ Convert a size in multiples of 32 bytes to a size in bytes
: SIZE> ( 32-bytes -- bytes ) 5 LSHIFT ;

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
  DUP HIGH-BLOCK-INDEX @ 2 PICK CEILING2 > IF
    NIP HIGH-BLOCK-SIZE @ LOG2
  ELSE
    DROP LOG2-CEILING
  THEN ;

\ Get the index into the array of sized free lists for a block size
: >INDEX-FILL ( 32-bytes allocator -- index )
  DUP HIGH-BLOCK-INDEX @ 2 PICK > IF
    NIP HIGH-BLOCK-SIZE @ LOG2
  ELSE
    DROP LOG2
  THEN ;

\ Get the header of a block
: BLOCK-HEADER ( addr -- block ) BLOCK-HEADER-SIZE - ;

\ Get the previous block
: PREV-BLOCK ( block -- block )
  DUP PREV-BLOCK-SIZE @ 0 > IF
    DUP DUP PREV-BLOCK-SIZE @ SIZE> - BLOCK-HEADER-SIZE -
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
    DUP BLOCK-SIZE W@ >INDEX-FILL CELLS ROT SIZED-BLOCKS @ +
    SWAP NEXT-SIZED-BLOCK @ SWAP !
  THEN ;

\ Add a block to its sized block list
: LINK-BLOCK ( block allocator -- )
  OVER 0 SWAP PREV-SIZED-BLOCK !
  OVER BLOCK-SIZE W@ >INDEX-FILL CELLS SWAP SIZED-BLOCKS @ +
  DUP @ IF
    2DUP @ PREV-SIZED-BLOCK !
    2DUP @ SWAP NEXT-SIZED-BLOCK !
  ELSE
    OVER 0 SWAP NEXT-SIZED-BLOCK !
  THEN
  ! ;

\ Update the succeeding block previous block size
: UPDATE-PREV-BLOCK-SIZE ( block -- )
  DUP BLOCK-SIZE W@ DUP SIZE> ROT BLOCK-HEADER-SIZE + + BLOCK-SIZE W! ;

\ Merge with a preceding block
: MERGE-PREV-BLOCK ( block allocator -- block )
  SWAP DUP PREV-BLOCK ?DUP IF
    BLOCK-FLAGS @ IN-USE 0 = IF
      DUP ROT UNLINK-BLOCK
      DUP BLOCK-SIZE W@ BLOCK-HEADER-SIZE >SIZE + OVER PREV-BLOCK BLOCK-SIZE W!
      PREV-BLOCK DUP UPDATE-PREV-BLOCK-SIZE
    ELSE
      2DROP
    THEN
  ELSE
    2DROP
  THEN ;

\ Merge with a succeeding block
: MERGE-NEXT-BLOCK ( block allocator -- )
  SWAP DUP NEXT-BLOCK BLOCK-FLAGS @ IN-USE 0 = IF
    DUP NEXT-BLOCK ROT UNLINK-BLOCK
    DUP DUP NEXT-BLOCK BLOCK-SIZE W@ BLOCK-HEADER-SIZE >SIZE +
    SWAP BLOCK-SIZE W!
    UPDATE-PREV-BLOCK-SIZE
  ELSE
    2DROP
  THEN ;

\ Free a block
: FREE-BLOCK ( block allocator -- )
  TUCK MERGE-PREV-BLOCK 2DUP SWAP MERGE-NEXT-BLOCK
  2DUP SWAP UNLINK-BLOCK SWAP LINK-BLOCK ;

\ Create an initial block
: INIT-BLOCK ( heap-size -- block )
  >SIZE CEILING2 ALIGNED HERE OVER BLOCK-HEADER-SIZE 2 * + ALLOT
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
  ALIGNED HERE ALLOCATOR-SIZE ALLOT
  OVER >SIZE CEILING2 OVER HIGH-BLOCK-SIZE !
  HERE OVER HIGH-BLOCK-SIZE @ LOG2 CELLS ALIGNED ALLOT
  OVER SIZED-BLOCKS !
  DUP SIZED-BLOCKS @ OVER HIGH-BLOCK-SIZE @ LOG2 CELLS 0 FILL
  OVER INIT-BLOCK
  ROT >SIZE 2 PICK >INDEX-FILL CELLS 2 PICK SIZED-BLOCKS @ + ! ;

BASE ! SET-CURRENT SET-ORDER