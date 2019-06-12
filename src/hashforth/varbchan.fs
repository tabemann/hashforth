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
forth-wordlist task-wordlist 2 set-order
task-wordlist set-current

begin-structure varbchan-size
  field: varbchan-bufbchan
  field: varbchan-send-mutex
  field: varbchan-recv-mutex
end-structure

begin-structure varbchan-header-size
  field: varbchan-remain-size
end-structure

\ Create a new variable-sized bounded channel, with the specified queue size,
\ entry size, condition variable queue size, send mutex queue size, and
\ receive mutex queue size.
: new-varbchan
  ( queue-size entry-size cond-size send-mutex-size recv-mutex-size -- chan )
  here varbchan-size allot
  tuck swap new-mutex swap varbchan-recv-mutex !
  tuck swap new-mutex swap varbchan-send-mutex !
  3 roll 3 roll varbchan-header-size + 3 roll
  new-bufbchan over varbchan-bufbchan ! ;

\ Get the size of the next packet.
: get-next-varbchan-size ( chan -- bytes )
  here swap varbchan-bufbchan @ peek-bufbchan
  here varbchan-remain-size @ ;

\ Get the maximum number of bytes to send per packet.
: get-varbchan-max-data-size ( chan -- bytes )
  varbchan-bufbchan @ get-bufbchan-entry-size varbchan-header-size - ;

\ Get the number of bytes to send per packet.
: get-varbchan-data-size ( bytes1 chan -- bytes2 )
  get-varbchan-max-data-size swap min ;

\ Advance HERE pointer
: advance-here ( chan -- )
  varbchan-bufbchan @ get-bufbchan-entry-size allot ;

\ Retract HERE pointer
: retract-here ( chan -- )
  varbchan-bufbchan @ get-bufbchan-entry-size negate allot ;

\ Actually send a packet.
: (send-varbchan) ( addr bytes chan -- )
  over 0 ?do
    over here varbchan-remain-size !
    2dup get-varbchan-data-size >r
    2 pick here varbchan-header-size + r@ cmove
    rot r@ + rot r> - rot
    here over varbchan-bufbchan @ 2 pick advance-here send-bufbchan
    dup retract-here
  dup get-varbchan-max-data-size +loop
  drop 2drop ;

\ Send a packet.
: send-varbchan ( addr bytes chan -- )
  dup varbchan-send-mutex @ ['] (send-varbchan) with-mutex ;

\ Actually receive a packet
: (recv-varbchan) ( addr bytes1 chan -- bytes2 )
  0 >r begin
    over 0 > if
      here over varbchan-bufbchan @ 2 pick advance-here recv-bufbchan
      dup retract-here
      here varbchan-header-size + 3 pick
      here varbchan-remain-size @ 3 pick get-varbchan-data-size
      4 pick min >r r@ cmove rot r@ + rot r@ - rot r> r> + >r
      here varbchan-remain-size @ over get-varbchan-max-data-size <=
    else
      here over varbchan-bufbchan @ recv-bufbchan
      here varbchan-remain-size @ over get-varbchan-max-data-size <=
    then
  until
  2drop drop r> ;

\ Receive a packet.
: recv-varbchan ( addr bytes1 chan -- bytes2 )
  dup varbchan-recv-mutex @ ['] (recv-varbchan) with-mutex ;

base ! set-current set-order