-- Table: public.entrada

-- DROP TABLE IF EXISTS public.entrada;

CREATE TABLE IF NOT EXISTS public.entrada
(
    id integer NOT NULL DEFAULT nextval('entrada_id_seq'::regclass),
    val_und numeric(9,2) NOT NULL,
    qtd integer NOT NULL,
    cupom character varying(255) COLLATE pg_catalog."default",
    produto bigint NOT NULL,
    CONSTRAINT entrada_pkey PRIMARY KEY (id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.entrada
    OWNER to postgres;

-- Table: public.saida

-- DROP TABLE IF EXISTS public.saida;

CREATE TABLE IF NOT EXISTS public.saida
(
    id bigint NOT NULL,
    valor numeric(9,2) NOT NULL,
    CONSTRAINT saida_pkey PRIMARY KEY (id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.saida
    OWNER to postgres;

truncate saida;
insert into saida (id, valor) 
select 
	id,
	valor * (1 - (desc_1 + desc_2)/100) as valor_desc
from (
	select 
		id,
		case when qtd > 9 then 10 else 0 end as desc_1,
		case when cupom like '%desc' then cast(substring(cupom, 0, 2) as int) else 0 end as desc_2,
		val_und * qtd as valor
	from entrada
) tmp;