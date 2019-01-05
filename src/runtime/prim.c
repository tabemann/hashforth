/* Copyright (c) 2018, Travis Bemann
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of the copyright holder nor the names of its
 *    contributors may be used to endorse or promote products derived from
 *    this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE. */

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <stdint.h>
#include "hf/common.h"
#include "hf/inner.h"

/* Forward declarations */

/* Register a primitive */
void hf_register_prim(hf_global_t* global, hf_full_token_t token, char* name,
		      hf_prim_t primitive, hf_flags_t flags,
		      hf_wordlist_id_t wordlist);

/* End primitive */
void hf_prim_end(hf_global_t* global);

/* NOP primitive */
void hf_prim_nop(hf_global_t* global);

/* EXIT primitive */
void hf_prim_exit(hf_global_t* global);

/* BRANCH primitive */
void hf_prim_branch(hf_global_t* global);

/* 0BRANCH primitive */
void hf_prim_0branch(hf_global_t* global);

/* (LIT) primitive */
void hf_prim_lit(hf_global_t* global);

/* (DATA) primitive */
void hf_prim_data(hf_global_t* global);

/* NEW-COLON primitive */
void hf_prim_new_colon(hf_global_t* global);

/* NEW-CREATE primitive */
void hf_prim_new_create(hf_global_t* global);

/* SET-DOES> primitive */
void hf_prim_set_does(hf_global_t* global);

/* FINISH primitive */
void hf_prim_finish(hf_global_t* global);

/* EXECUTE primitive */
void hf_prim_execute(hf_global_t* global);

/* DROP primitive */
void hf_prim_drop(hf_global_t* global);

/* DUP primitive */
void hf_prim_dup(hf_global_t* global);

/* SWAP primitive */
void hf_prim_swap(hf_global_t* global);

/* ROT primitive */
void hf_prim_rot(hf_global_t* global);

/* PICK primitive */
void hf_prim_pick(hf_global_t* global);

/* ROLL primitive */
void hf_prim_roll(hf_global_t* global);

/* @ primitive */
void hf_prim_load(hf_global_t* global);

/* ! primitive */
void hf_prim_store(hf_global_t* global);

/* C@ primitive */
void hf_prim_load_8(hf_global_t* global);

/* C! primitive */
void hf_prim_store_8(hf_global_t* global);

/* = primitive */
void hf_prim_eq(hf_global_t* global);

/* <> primitive */
void hf_prim_ne(hf_global_t* global);

/* < primitive */
void hf_prim_lt(hf_global_t* global);

/* > primitive */
void hf_prim_gt(hf_global_t* global);

/* U< primitive */
void hf_prim_ult(hf_global_t* global);

/* U> primitive */
void hf_prim_ugt(hf_global_t* global);

/* NOT primitive */
void hf_prim_not(hf_global_t* global);

/* AND primitive */
void hf_prim_and(hf_global_t* global);

/* OR primitive */
void hf_prim_or(hf_global_t* global);

/* XOR primitive */
void hf_prim_xor(hf_global_t* global);

/* LSHIFT primitive */
void hf_prim_lshift(hf_global_t* global);

/* RSHIFT primitive */
void hf_prim_rshift(hf_global_t* global);

/* ARSHIFT primitive */
void hf_prim_arshift(hf_global_t* global);

/* + primitive */
void hf_prim_add(hf_global_t* global);

/* - primitive */
void hf_prim_sub(hf_global_t* global);

/* * primitive */
void hf_prim_mul(hf_global_t* global);

/* / primitive */
void hf_prim_div(hf_global_t* global);

/* MOD primitive */
void hf_prim_mod(hf_global_t* global);

/* U/ primitive */
void hf_prim_udiv(hf_global_t* global);

/* UMOD primitive */
void hf_prim_umod(hf_global_t* global);

/* R@ primitive */
void hf_prim_load_r(hf_global_t* global);

/* >R primitive */
void hf_prim_store_r(hf_global_t* global);

/* R> primitive */
void hf_prim_pop_r(hf_global_t* global);

/* HERE primitive */
void hf_prim_load_here(hf_global_t* global);

/* HERE! primitive */
void hf_prim_store_here(hf_global_t* global);

/* SP@ primitive */
void hf_prim_load_sp(hf_global_t* global);

/* SP! primitive */
void hf_prim_store_sp(hf_global_t* global);

/* RP@ primitive */
void hf_prim_load_rp(hf_global_t* global);

/* RP! primitive */
void hf_prim_store_rp(hf_global_t* global);

/* >BODY primitive */
void hf_prim_to_body(hf_global_t* global);

/* WORD>NAME primitive */
void hf_prim_word_to_name(hf_global_t* global);

/* NAME>WORD primitive */
void hf_prim_name_to_word(hf_global_t* global);

/* WORD>NEXT primitive */
void hf_prim_word_to_next(hf_global_t* global);

/* WORDLIST>FIRST primitive */
void hf_prim_wordlist_to_first(hf_global_t* global);

/* GET-CURRENT primitive */
void hf_prim_get_current(hf_global_t* global);

/* SET-CURRENT primitive */
void hf_prim_set_current(hf_global_t* global);

/* WORD>FLAGS primitive */
void hf_prim_word_to_flags(hf_global_t* global);

/* FLAGS>WORD primitive */
void hf_prim_flags_to_word(hf_global_t* global);

/* WORDLIST primitive */
void hf_prim_wordlist(hf_global_t* global);

/* HALF-TOKEN-SIZE primitive */
void hf_prim_half_token_size(hf_global_t* global);

/* FULL-TOKEN-SIZE primitive */
void hf_prim_full_token_size(hf_global_t* global);

/* TOKEN-FLAG-BIT primitive */
void hf_prim_token_flag_bit(hf_global_t* global);

/* CELL-SIZE primitive */
void hf_prim_cell_size(hf_global_t* global);

/* H@ primitive */
void hf_prim_load_16(hf_global_t* global);

/* H! primitive */
void hf_prim_store_16(hf_global_t* global);

/* W@ primitive */
void hf_prim_load_32(hf_global_t* global);

/* W! primitive */
void hf_prim_store_32(hf_global_t* global);

/* TYPE primitive */
void hf_prim_type(hf_global_t* global);

/* KEY primitive */
void hf_prim_key(hf_global_t* global);

/* ACCEPT primitive */
void hf_prim_accept(hf_global_t* global);

/* Macros */

/* Convert a C boolean into a Forth boolean */
#define HF_BOOL(value) ((value) ? HF_TRUE : HF_FALSE)

/* Definitions */

/* Register a primitive */
void hf_register_prim(hf_global_t* global, hf_full_token_t token, char* name,
		      hf_prim_t primitive, hf_flags_t flags,
		      hf_wordlist_id_t wordlist) {
  hf_cell_t name_length = 0;
  void* name_space = NULL;
  hf_word_t* word;
  if(name) {
    hf_new_user_space(global);
    name_length = strlen(name);
    name_space = hf_allocate(global, name_length);
    memcpy(name_space, name, name_length);
  }
  word = hf_new_word(global, token);
  word->flags = flags;
  word->name_length = name_length;
  word->name = name_space;
  word->data = NULL;
  word->primitive = primitive;
  word->secondary = NULL;
  word->next = global->wordlists[wordlist].first;
  global->wordlists[wordlist].first = token;
}

/* Register primitives */
void hf_register_prims(hf_global_t* global) {
  hf_register_prim(global, HF_PRIM_END, "END", hf_prim_end,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_NOP, "NOP", hf_prim_nop,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_EXIT, "EXIT", hf_prim_exit,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_BRANCH, "BRANCH", hf_prim_branch,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_0BRANCH, "0BRANCH", hf_prim_0branch,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LIT, "(LIT)", hf_prim_lit,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_DATA, "(DATA)", hf_prim_data,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_NEW_COLON, "NEW-COLON", hf_prim_new_colon,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_NEW_CREATE, "NEW-CREATE", hf_prim_new_create,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_SET_DOES, "SET-DOES>", hf_prim_set_does,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_FINISH, "FINISH", hf_prim_finish,
		   HF_WORD_IMMEDIATE, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_EXECUTE, "EXECUTE", hf_prim_execute,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_DROP, "DROP", hf_prim_drop,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_DUP, "DUP", hf_prim_dup,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_SWAP, "SWAP", hf_prim_swap,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_ROT, "ROT", hf_prim_rot,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_PICK, "PICK", hf_prim_pick,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_ROLL, "ROLL", hf_prim_roll,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LOAD, "@", hf_prim_load,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_STORE, "!", hf_prim_store,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LOAD_8, "C@", hf_prim_load_8,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_STORE_8, "C!", hf_prim_store_8,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_EQ, "=", hf_prim_eq,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_NE, "<>", hf_prim_ne,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LT, "<", hf_prim_lt,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_GT, ">", hf_prim_gt,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_ULT, "U<", hf_prim_ult,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_UGT, "U>", hf_prim_ugt,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_NOT, "NOT", hf_prim_not,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_AND, "AND", hf_prim_and,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_OR, "OR", hf_prim_or,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_XOR, "XOR", hf_prim_xor,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LSHIFT, "LSHIFT", hf_prim_lshift,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_RSHIFT, "RSHIFT", hf_prim_rshift,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_ARSHIFT, "ARSHIFT", hf_prim_arshift,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_ADD, "+", hf_prim_add,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_SUB, "-", hf_prim_sub,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_MUL, "*", hf_prim_mul,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_DIV, "/", hf_prim_div,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_MOD, "MOD", hf_prim_mod,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_UDIV, "U/", hf_prim_udiv,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_UMOD, "UMOD", hf_prim_umod,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LOAD_R, "R@", hf_prim_load_r,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_STORE_R, ">R", hf_prim_store_r,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_POP_R, "R>", hf_prim_pop_r,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LOAD_HERE, "HERE", hf_prim_load_here,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_STORE_HERE, "HERE!", hf_prim_store_here,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LOAD_SP, "SP@", hf_prim_load_sp,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_STORE_SP, "SP!", hf_prim_store_sp,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LOAD_RP, "RP@", hf_prim_load_rp,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_STORE_RP, "RP!", hf_prim_store_rp,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_TO_BODY, ">BODY", hf_prim_to_body,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_WORD_TO_NAME, "WORD>NAME",
		   hf_prim_word_to_name, HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_NAME_TO_WORD, "NAME>WORD",
		   hf_prim_name_to_word, HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_WORD_TO_NEXT, "WORD>NEXT",
		   hf_prim_word_to_next, HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_WORDLIST_TO_FIRST, "WORDLIST>FIRST",
		   hf_prim_wordlist_to_first, HF_WORD_NORMAL,
		   HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_GET_CURRENT, "GET-CURRENT",
		   hf_prim_get_current, HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_SET_CURRENT, "SET-CURRENT",
		   hf_prim_set_current, HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_WORD_TO_FLAGS, "WORD>FLAGS",
		   hf_prim_word_to_flags, HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_FLAGS_TO_WORD, "FLAGS>WORD",
		   hf_prim_flags_to_word, HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_WORDLIST, "WORDLIST", hf_prim_wordlist,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_HALF_TOKEN_SIZE, "HALF-TOKEN-SIZE",
		   hf_prim_half_token_size, HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_FULL_TOKEN_SIZE, "FULL-TOKEN-SIZE",
		   hf_prim_full_token_size, HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_TOKEN_FLAG_BIT, "TOKEN-FLAG-BIT",
		   hf_prim_token_flag_bit, HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_CELL_SIZE, "CELL-SIZE", hf_prim_cell_size,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LOAD_16, "H@", hf_prim_load_16,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_STORE_16, "H!", hf_prim_store_16,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_LOAD_32, "W@", hf_prim_load_32,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_STORE_32, "W!", hf_prim_store_32,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_TYPE, "TYPE", hf_prim_type, HF_WORD_NORMAL,
		   HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_KEY, "KEY", hf_prim_key, HF_WORD_NORMAL,
		   HF_WORDLIST_FORTH);
  hf_register_prim(global, HF_PRIM_ACCEPT, "ACCEPT", hf_prim_accept,
		   HF_WORD_NORMAL, HF_WORDLIST_FORTH);
}

/* Enter primitive */
void hf_prim_enter(hf_global_t* global) {
  *(--global->return_stack) = global->ip;
  global->ip = global->current_word->secondary;
}

/* Do CREATE primitive */
void hf_prim_do_create(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)global->current_word->data;
}

/* Do DOES> primitive */
void hf_prim_do_does(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)global->current_word->data;
  *(--global->return_stack) = global->ip;
  global->ip = global->current_word->secondary;
}

/* End primitive */
void hf_prim_end(hf_global_t* global) {
  fprintf(stderr, "End should never be reached!\n");
  abort();
}

/* NOP primitive */
void hf_prim_nop(hf_global_t* global) {
}

/* EXIT primitive */
void hf_prim_exit(hf_global_t* global) {
  global->ip = *global->return_stack++;
}

/* BRANCH primitive */
void hf_prim_branch(hf_global_t* global) {
  global->ip = *(hf_token_t**)global->ip;
}

/* 0BRANCH primitive */
void hf_prim_0branch(hf_global_t* global) {
  if(!(*global->data_stack++)) {
    global->ip = *(hf_token_t**)global->ip;
  } else {
    *(void**)(&global->ip) += sizeof(hf_token_t*);
  }
}

/* (LIT) primitive */
void hf_prim_lit(hf_global_t* global) {
  *(--global->data_stack) = *(hf_cell_t*)global->ip;
  global->ip = (hf_token_t*)((void*)global->ip + sizeof(hf_cell_t));
}

/* (DATA) primitive */
void hf_prim_data(hf_global_t* global) {
  hf_cell_t bytes = *(hf_cell_t*)global->ip;
  *(--global->data_stack) = (hf_cell_t)global->ip;
  global->ip = (hf_token_t*)((void*)global->ip + sizeof(hf_cell_t) + bytes);
}

/* NEW-COLON primitive */
void hf_prim_new_colon(hf_global_t* global) {
  hf_word_t* word;
  hf_full_token_t token = hf_new_token(global);
  hf_new_user_space(global);
  word = hf_new_word(global, token);
  word->flags = 0;
  word->name_length = 0;
  word->name = NULL;
  word->data = NULL;
  word->primitive = hf_prim_enter;
  word->secondary = global->user_space_current;
  word->next = global->wordlists[global->current_wordlist].first;
  global->wordlists[global->current_wordlist].first = token;
  *(--global->data_stack) = (hf_cell_t)token;
}

/* NEW-CREATE primitive */
void hf_prim_new_create(hf_global_t* global) {
  hf_word_t* word;
  hf_full_token_t token = hf_new_token(global);
  hf_new_user_space(global);
  word = hf_new_word(global, token);
  word->flags = 0;
  word->name_length = 0;
  word->name = NULL;
  word->data = global->user_space_current;
  word->primitive = hf_prim_do_create;
  word->secondary = NULL;
  word->next = global->wordlists[global->current_wordlist].first;
  global->wordlists[global->current_wordlist].first = token;
  *(--global->data_stack) = (hf_cell_t)token;
}

/* SET-DOES> primitive */
void hf_prim_set_does(hf_global_t* global) {
  hf_cell_t token = *global->data_stack++;
  if(token < global->word_count) {
    hf_word_t* word = global->words + token;
    word->primitive = hf_prim_do_does;
    word->secondary = global->ip;
    global->ip = *global->return_stack++;
  } else {
    fprintf(stderr, "Out of range token!\n");
    abort();
  }
}

/* FINISH primitive */
void hf_prim_finish(hf_global_t* global) {
  hf_cell_t token = *global->data_stack++;
  if(token < global->word_count) {
    // In a practical implementation, something will be done here.
  } else {
    fprintf(stderr, "Out of range token!\n");
  }
}

/* EXECUTE primitive */
void hf_prim_execute(hf_global_t* global) {
  hf_cell_t token = *global->data_stack++;
  if(token < global->word_count) {
    hf_word_t* word = global->words + token;
    global->current_word = word;
    word->primitive(global);
  } else {
    fprintf(stderr, "Out of range token!\n");
    abort();
  }
}

/* DROP primitive */
void hf_prim_drop(hf_global_t* global) {
  global->data_stack++;
}

/* DUP primitive */
void hf_prim_dup(hf_global_t* global) {
  hf_cell_t value = *global->data_stack;
  *(--global->data_stack) = value;
}

/* SWAP primitive */
void hf_prim_swap(hf_global_t* global) {
  hf_cell_t value = *global->data_stack;
  *global->data_stack = *(global->data_stack + 1);
  *(global->data_stack + 1) = value;
}

/* ROT primitive */
void hf_prim_rot(hf_global_t* global) {
  hf_cell_t value0 = *global->data_stack;
  hf_cell_t value1 = *(global->data_stack + 1);
  hf_cell_t value2 = *(global->data_stack + 2);
  *global->data_stack = value2;
  *(global->data_stack + 1) = value0;
  *(global->data_stack + 2) = value1;
}

/* PICK primitive */
void hf_prim_pick(hf_global_t* global) {
  *global->data_stack = *(global->data_stack + *global->data_stack + 1);
}

/* ROLL primitive */
void hf_prim_roll(hf_global_t* global) {
  hf_cell_t count = *global->data_stack++;
  hf_cell_t end_value = *(global->data_stack + count);
  while(count) {
    *(global->data_stack + count) = *(global->data_stack + count - 1);
    count--;
  }
  *global->data_stack = end_value;
}

/* @ primitive */
void hf_prim_load(hf_global_t* global) {
  *global->data_stack = *(hf_cell_t*)(*global->data_stack);
}

/* ! primitive */
void hf_prim_store(hf_global_t* global) {
  *(hf_cell_t*)(*global->data_stack) = *(global->data_stack + 1);
  global->data_stack += 2;
}

/* C@ primitive */
void hf_prim_load_8(hf_global_t* global) {
  *global->data_stack = *(hf_byte_t*)(*global->data_stack);
}

/* C! primitive */
void hf_prim_store_8(hf_global_t* global) {
  *(hf_byte_t*)(*global->data_stack) = *(global->data_stack + 1) & 0xFF;
  global->data_stack += 2;
}

/* = primitive */
void hf_prim_eq(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(global->data_stack + 1) == *global->data_stack);
  *(++global->data_stack) = comparison;
}

/* <> primitive */
void hf_prim_ne(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(global->data_stack + 1) != *global->data_stack);
  *(++global->data_stack) = comparison;
}

/* < primitive */
void hf_prim_lt(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(hf_sign_cell_t*)(global->data_stack + 1) <
	    *(hf_sign_cell_t*)global->data_stack);
  *(++global->data_stack) = comparison;
}

/* > primitive */
void hf_prim_gt(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(hf_sign_cell_t*)(global->data_stack + 1) >
	    *(hf_sign_cell_t*)global->data_stack);
  *(++global->data_stack) = comparison;
}

/* U< primitive */
void hf_prim_ult(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(global->data_stack + 1) < *global->data_stack);
  *(++global->data_stack) = comparison;
}

/* U> primitive */
void hf_prim_ugt(hf_global_t* global) {
  hf_cell_t comparison =
    HF_BOOL(*(global->data_stack + 1) > *global->data_stack);
  *(++global->data_stack) = comparison;
}

/* NOT primitive */
void hf_prim_not(hf_global_t* global) {
  *global->data_stack = ~(*global->data_stack);
}

/* AND primitive */
void hf_prim_and(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) & *global->data_stack;
  *(++global->data_stack) = value;
}

/* OR primitive */
void hf_prim_or(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) | *global->data_stack;
  *(++global->data_stack) = value;
}

/* XOR primitive */
void hf_prim_xor(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) ^ *global->data_stack;
  *(++global->data_stack) = value;
}

/* LSHIFT primitive */
void hf_prim_lshift(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) << *global->data_stack;
  *(++global->data_stack) = value;
}

/* RSHIFT primitive */
void hf_prim_rshift(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) >> *global->data_stack;
  *(++global->data_stack) = value;
}

/* ARSHIFT primitive */
void hf_prim_arshift(hf_global_t* global) {
  hf_sign_cell_t value = *(hf_sign_cell_t*)(global->data_stack + 1) >>
    *global->data_stack;
  *(hf_sign_cell_t*)(++global->data_stack) = value;
}

/* + primitive */
void hf_prim_add(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) + *global->data_stack;
  *(++global->data_stack) = value;
}

/* - primitive */
void hf_prim_sub(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) - *global->data_stack;
  *(++global->data_stack) = value;
}

/* * primitive */
void hf_prim_mul(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) * *global->data_stack;
  *(++global->data_stack) = value;
}

/* / primitive */
void hf_prim_div(hf_global_t* global) {
  hf_sign_cell_t value = *(hf_sign_cell_t*)(global->data_stack + 1) /
    *(hf_sign_cell_t*)global->data_stack;
  *(hf_sign_cell_t*)(++global->data_stack) = value;
}

/* MOD primitive */
void hf_prim_mod(hf_global_t* global) {
  hf_sign_cell_t value = *(hf_sign_cell_t*)(global->data_stack + 1) %
    *(hf_sign_cell_t*)global->data_stack;
  *(hf_sign_cell_t*)(++global->data_stack) = value;
}

/* U/ primitive */
void hf_prim_udiv(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) / *global->data_stack;
  *(++global->data_stack) = value;
}

/* UMOD primitive */
void hf_prim_umod(hf_global_t* global) {
  hf_cell_t value = *(global->data_stack + 1) % *global->data_stack;
  *(++global->data_stack) = value;
}

/* R@ primitive */
void hf_prim_load_r(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)(*global->return_stack);
}

/* >R primitive */
void hf_prim_store_r(hf_global_t* global) {
  *(--global->return_stack) = (hf_token_t*)(*global->data_stack++);
}

/* R> primitive */
void hf_prim_pop_r(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)(*global->return_stack++);
}

/* HERE primitive */
void hf_prim_load_here(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)global->user_space_current;
}

/* HERE! primitive */
void hf_prim_store_here(hf_global_t* global) {
  global->user_space_current = (void*)(*global->data_stack++);
}

/* SP@ primitive */
void hf_prim_load_sp(hf_global_t* global) {
  hf_cell_t* data_stack = global->data_stack;
  *(--global->data_stack) = (hf_cell_t)data_stack;
}

/* SP! primitive */
void hf_prim_store_sp(hf_global_t* global) {
  global->data_stack = (hf_cell_t*)(*global->data_stack);  
}

/* RP@ primitive */
void hf_prim_load_rp(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)global->return_stack;
}

/* RP! primitive */
void hf_prim_store_rp(hf_global_t* global) {
  global->return_stack = (hf_token_t**)(*global->data_stack++);
}

/* >BODY primitive */
void hf_prim_to_body(hf_global_t* global) {
  hf_cell_t token = *global->data_stack;
  if(token < global->word_count) {
    *global->data_stack = (hf_cell_t)global->words[token].data;
  } else {
    fprintf(stderr, "Out of range token!\n");
    abort();
  }
}

/* WORD>NAME primitive */
void hf_prim_word_to_name(hf_global_t* global) {
  hf_cell_t token = *global->data_stack;
  if(token < global->word_count) {
    hf_word_t* word = global->words + token;
    *global->data_stack = (hf_cell_t)word->name;
    *(--global->data_stack) = word->name_length;
  } else {
    fprintf(stderr, "Out of range token!\n");
    abort();
  }
}

/* NAME>WORD primitive */
void hf_prim_name_to_word(hf_global_t* global) {
  hf_cell_t token = *global->data_stack++;
  hf_cell_t name_length = *global->data_stack++;
  hf_byte_t* name = (hf_byte_t*)(*global->data_stack++);
  if(token < global->word_count) {
    hf_word_t* word = global->words + token;
    word->name_length = name_length;
    word->name = name;
  } else {
    fprintf(stderr, "Out of range token!\n");
    abort();
  }
}

/* WORD>NEXT primitive */
void hf_prim_word_to_next(hf_global_t* global) {
  hf_cell_t token = *global->data_stack;
  if(token < global->word_count) {
    hf_word_t* word = global->words + token;
    *global->data_stack = word->next;
  } else {
    fprintf(stderr, "Out of range token!\n");
    abort();
  }
}

/* WORDLIST>FIRST primitive */
void hf_prim_wordlist_to_first(hf_global_t* global) {
  hf_cell_t id = *global->data_stack;
  if(id < global->wordlist_count) {
    hf_wordlist_t* wordlist = global->wordlists + id;
    *global->data_stack = wordlist->first;
  } else {
    fprintf(stderr, "Out of range wordlist ID\n");
    abort();
  }
}

/* GET-CURRENT primitive */
void hf_prim_get_current(hf_global_t* global) {
  *(--global->data_stack) = (hf_cell_t)global->current_wordlist;
}

/* SET-CURRENT primitive */
void hf_prim_set_current(hf_global_t* global) {
  global->current_wordlist = (hf_wordlist_id_t)(*global->data_stack++);
}

/* WORD>FLAGS primitive */
void hf_prim_word_to_flags(hf_global_t* global) {
  hf_cell_t token = *global->data_stack;
  if(token < global->word_count) {
    hf_word_t* word = global->words + token;
    *global->data_stack = word->flags;
  } else {
    fprintf(stderr, "Out of range token!\n");
    abort();
  }  
}

/* FLAGS>WORD primitive */
void hf_prim_flags_to_word(hf_global_t* global) {
  hf_cell_t token = *global->data_stack++;
  hf_flags_t flags = *global->data_stack++;
  if(token < global->word_count) {
    hf_word_t* word = global->words + token;
    word->flags = flags;
  } else {
    fprintf(stderr, "Out of range token!\n");
    abort();
  }
}

/* WORDLIST primitive */
void hf_prim_wordlist(hf_global_t* global) {
  *(--global->data_stack) = hf_new_wordlist(global);
}

/* HALF-TOKEN-SIZE primitive */
void hf_prim_half_token_size(hf_global_t* global) {
  *(--global->data_stack) = sizeof(hf_token_t);
}

/* FULL-TOKEN-SIZE primitive */
void hf_prim_full_token_size(hf_global_t* global) {
  *(--global->data_stack) = sizeof(hf_full_token_t);
}

/* TOKEN-FLAG-BIT primitive */
void hf_prim_token_flag_bit(hf_global_t* global) {
#ifdef TOKEN_8_16
  *(--global->data_stack) = HF_TRUE;
#else
#ifdef TOKEN_16_32
  *(--global->data_stack) = HF_TRUE;
#else
  *(--global->data_stack) = HF_FALSE;
#endif
#endif
}

/* CELL-SIZE primitive */
void hf_prim_cell_size(hf_global_t* global) {
  *(--global->data_stack) = sizeof(hf_cell_t);
}

/* H@ primitive */
void hf_prim_load_16(hf_global_t* global) {
  *global->data_stack = *(uint16_t*)(*global->data_stack);
}

/* H! primitive */
void hf_prim_store_16(hf_global_t* global) {
  *(uint16_t*)(*global->data_stack) = *(global->data_stack + 1) & 0xFFFF;
  global->data_stack += 2;
}

/* W@ primitive */
void hf_prim_load_32(hf_global_t* global) {
#ifndef CELL_16
  *global->data_stack = *(uint32_t*)(*global->data_stack);
#else
  fprintf(stderr, "32-bit values not supported!\n");
#endif
}

/* W! primitive */
void hf_prim_store_32(hf_global_t* global) {
#ifndef CELL_16
  *(uint32_t*)(*global->data_stack) = *(global->data_stack + 1) & 0xFFFFFFFF;
  global->data_stack += 2;
#else
  fprintf(stderr, "32-bit values not supported!\n");
#endif
}

/* TYPE primitive */
void hf_prim_type(hf_global_t* global) {
  hf_cell_t length = *global->data_stack++;
  hf_byte_t* buffer = (hf_byte_t*)(*global->data_stack++);
  fwrite(buffer, sizeof(hf_byte_t), length, stdout);
}

/* KEY primitive */
void hf_prim_key(hf_global_t* global) {
  int key = fgetc(stdin);
  if(key != EOF) {
    *(--global->data_stack) = key;
  } else {
    exit(0);
  }
}

/* ACCEPT primitive */
void hf_prim_accept(hf_global_t* global) {
  hf_cell_t buffer_size = *global->data_stack++;
  hf_byte_t* buffer = (hf_byte_t*)(*global->data_stack);
  hf_byte_t* data_read = fgets(buffer, buffer_size, stdin);
  if(data_read) {
    hf_cell_t data_read_length = strlen(data_read);
    if(data_read[data_read_length - 1] == '\n') {
      data_read_length--;
    }
    *global->data_stack = data_read_length;
  } else {
    *global->data_stack = 0;
  }
}
