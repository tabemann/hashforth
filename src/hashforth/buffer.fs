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

base @ get-current get-order

decimal
forth-wordlist 1 set-order
forth-wordlist set-current

wordlist constant buffer-wordlist
forth-wordlist buffer-wordlist 2 set-order
buffer-wordlist set-current

begin-structure buffer-size
  field: buffer-data
  field: buffer-data-count
  field: buffer-data-size
end-structure

: allocate-buffer ( init-bytes -- buffer )
  buffer-size allocate!
  over allocate! over buffer-data !
  tuck buffer-data-size !
  0 over buffer-data-count ! ;

: new-buffer allocate-buffer ;

: destroy-buffer ( buffer -- ) dup buffer-data @ free! free! ;

: get-buffer-length ( buffer -- bytes ) buffer-data-count @ ;

: expand-buffer ( bytes buffer -- )
  dup buffer-data-count @ rot + over buffer-data-size @ 2 * max
  over buffer-data @ over resize! 2 pick buffer-data !
  swap buffer-data-size ! ;

: append-buffer ( c-addr bytes buffer -- )
  dup buffer-data-count @ 2 pick + over buffer-data-size @ > if
    2dup expand-buffer
  then
  dup buffer-data @ over buffer-data-count @ + 3 roll swap 3 pick move
  buffer-data-count +! ;

: append-byte-buffer ( c buffer -- )
  dup buffer-data-count @ over buffer-data-size @ >= if
    1 over expand-buffer
  then
  dup buffer-data @ over buffer-data-count @ + rot swap c!
  1 swap buffer-data-count +! ;

: remove-buffer ( bytes offset buffer -- )
  dup buffer-data @ 3 pick + 2 pick + over buffer-data @ 3 pick +
  2 pick buffer-data-count @ 5 pick - move
  rot over buffer-data-count @ swap - swap buffer-data-count ! drop ;

: remove-start-buffer ( bytes buffer -- )
  dup buffer-data @ 2 pick + over buffer-data @ 2 pick buffer-data-count @
  4 pick - move dup buffer-data-count @ rot - swap buffer-data-count ! ;

: remove-end-buffer ( bytes buffer -- )
  dup buffer-data-count @ rot - swap buffer-data-count ! ;

: prepend-buffer ( c-addr bytes buffer -- )
  dup buffer-data-count @ 2 pick + over buffer-data-size @ > if
    2dup expand-buffer
  then
  dup buffer-data @ over buffer-data @ 3 pick +
  2 pick buffer-data-count @ move 2 pick over buffer-data @ 3 pick move
  dup buffer-data-count @ rot + swap buffer-data-count ! drop ;

: prepend-byte-buffer ( c buffer -- )
  dup buffer-data-count @ over buffer-data-size @ >= if
    1 over expand-buffer
  then
  dup buffer-data @ over buffer-data @ 1+
  2 pick buffer-data-count @ move tuck buffer-data @ c!
  dup buffer-data-count @ 1+ swap buffer-data-count ! ;

: write-buffer ( c-addr bytes offset buffer -- )
  over 3 pick + over buffer-data-size @ > if
    over 3 pick + over buffer-data-count @ - over expand-buffer
  then
  3 roll over buffer-data @ 3 pick + 4 pick move
  2 pick 2 pick + over buffer-data-count @ > if
    rot rot + swap buffer-data-count !
  else
    2drop drop
  then ;

: write-byte-buffer ( c offset buffer -- )
  over 1+ over buffer-data-size @ > if
    over 1+ over buffer-data-count @ - over expand-buffer
  then
  rot over buffer-data @ 3 pick + c!
  over 1+ over buffer-data-count @ > if
    over 1+ swap buffer-data-count !
  else
    2drop
  then ;

: insert-buffer ( c-addr bytes offset buffer -- )
  dup buffer-data-count @ 2 pick >= if
    write-buffer
  else
    dup buffer-data-count @ 3 pick + over buffer-data-size @ > if
      2 pick over expand-buffer
    then
    dup buffer-data @ 2 pick + over buffer-data @ 3 pick + 4 pick +
    2 pick buffer-data-count @ 4 pick - move
    3 pick over buffer-data @ 3 pick + 4 pick move
    dup buffer-data-count @ 3 roll + swap buffer-data-count ! 2drop
  then ;

: insert-byte-buffer ( c offset buffer -- )
  dup buffer-data-count @ 2 pick >= if
    write-byte-buffer
  else
    dup buffer-data-count @ over buffer-data-size @ >= if
      1 over expand-buffer
    then
    dup buffer-data @ 2 pick + over buffer-data @ 3 pick + 1+
    2 pick buffer-data-count @ 4 pick - move
    rot over buffer-data @ 3 pick + c!
    dup buffer-data-count @ 1+ swap buffer-data-count ! drop
  then ;

: get-buffer ( buffer -- c-addr bytes )
  dup buffer-data @ swap buffer-data-count @ ;

: clear-buffer ( buffer -- ) 0 swap buffer-data-count ! ;

set-order set-current base !