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

include src/hashforth/sixel.fs

forth-wordlist sixel-wordlist 2 set-order

16 constant color-count
16 constant color-width
36 constant height
color-count color-width * height color-count new-sixel-fb constant fb

: red-gradient ( index -- red ) color-count swap - 255 * color-count / ;

: green-gradient ( index -- green )
  dup color-count 2 / < if
    255 * color-count 2 / /
  else
    color-count swap - 255 * color-count 2 / /
  then ;

: blue-gradient ( index -- blue ) 255 * color-count / ;

: fill-palette ( -- )
  color-count 0 ?do
    i red-gradient i green-gradient i blue-gradient i fb set-color
  loop ;

: fill-gradient ( -- )
  fb clear-pixels
  color-count 0 ?do
    i color-width * dup color-width + swap ?do
      0 begin dup height < while i over j fb pixel-no-clear! 1 + repeat drop
    loop
  loop ;

: draw-gradient ( -- ) fill-palette fill-gradient fb draw ;

draw-gradient
