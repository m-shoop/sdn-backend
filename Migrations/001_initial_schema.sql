--
-- PostgreSQL database dump
--

\restrict 3rh0D2EeDdrgdm62ig3FhUpZtsz1uzN8det1AiqgAbYJBoMLBVv0qgE8FFU9EKB

-- Dumped from database version 16.11 (Ubuntu 16.11-0ubuntu0.24.04.1)
-- Dumped by pg_dump version 16.11 (Ubuntu 16.11-0ubuntu0.24.04.1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: core; Type: SCHEMA; Schema: -; Owner: shooperdooper
--

CREATE SCHEMA core;


ALTER SCHEMA core OWNER TO shooperdooper;

--
-- Name: status; Type: TYPE; Schema: core; Owner: shooperdooper
--

CREATE TYPE core.status AS ENUM (
    'pending',
    'confirmed',
    'expired',
    'canceled'
);


ALTER TYPE core.status OWNER TO shooperdooper;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: accounts; Type: TABLE; Schema: core; Owner: shooperdooper
--

CREATE TABLE core.accounts (
    account_id integer NOT NULL,
    name character varying,
    email character varying,
    role character varying,
    password_hash text,
    password_reset_token_hash text,
    password_reset_token_expires timestamp with time zone,
    email_notifications_enabled boolean DEFAULT true NOT NULL,
    last_activity timestamp with time zone
);


ALTER TABLE core.accounts OWNER TO shooperdooper;

--
-- Name: accounts_account_id_seq; Type: SEQUENCE; Schema: core; Owner: shooperdooper
--

CREATE SEQUENCE core.accounts_account_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE core.accounts_account_id_seq OWNER TO shooperdooper;

--
-- Name: accounts_account_id_seq; Type: SEQUENCE OWNED BY; Schema: core; Owner: shooperdooper
--

ALTER SEQUENCE core.accounts_account_id_seq OWNED BY core.accounts.account_id;


--
-- Name: agreements; Type: TABLE; Schema: core; Owner: shooperdooper
--

CREATE TABLE core.agreements (
    id integer NOT NULL,
    date date,
    start_time time without time zone,
    service_id integer,
    tech_id integer,
    client_id integer,
    salon_id integer,
    expires_at timestamp without time zone,
    confirmed_at timestamp without time zone,
    confirmation_token_hash character varying,
    creation_timestamp timestamp without time zone,
    status integer
);


ALTER TABLE core.agreements OWNER TO shooperdooper;

--
-- Name: agreements_agreement_id_seq; Type: SEQUENCE; Schema: core; Owner: shooperdooper
--

CREATE SEQUENCE core.agreements_agreement_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE core.agreements_agreement_id_seq OWNER TO shooperdooper;

--
-- Name: agreements_agreement_id_seq; Type: SEQUENCE OWNED BY; Schema: core; Owner: shooperdooper
--

ALTER SEQUENCE core.agreements_agreement_id_seq OWNED BY core.agreements.id;


--
-- Name: day_time_range; Type: TABLE; Schema: core; Owner: shooperdooper
--

CREATE TABLE core.day_time_range (
    day_time_range_id integer NOT NULL,
    schedule_id integer,
    day_of_week integer NOT NULL,
    start_time time without time zone NOT NULL,
    end_time time without time zone NOT NULL
);


ALTER TABLE core.day_time_range OWNER TO shooperdooper;

--
-- Name: day_time_range_day_time_range_id_seq; Type: SEQUENCE; Schema: core; Owner: shooperdooper
--

CREATE SEQUENCE core.day_time_range_day_time_range_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE core.day_time_range_day_time_range_id_seq OWNER TO shooperdooper;

--
-- Name: day_time_range_day_time_range_id_seq; Type: SEQUENCE OWNED BY; Schema: core; Owner: shooperdooper
--

ALTER SEQUENCE core.day_time_range_day_time_range_id_seq OWNED BY core.day_time_range.day_time_range_id;


--
-- Name: salons; Type: TABLE; Schema: core; Owner: shooperdooper
--

CREATE TABLE core.salons (
    salon_id integer NOT NULL,
    time_zone_info character varying
);


ALTER TABLE core.salons OWNER TO shooperdooper;

--
-- Name: salons_salon_id_seq; Type: SEQUENCE; Schema: core; Owner: shooperdooper
--

CREATE SEQUENCE core.salons_salon_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE core.salons_salon_id_seq OWNER TO shooperdooper;

--
-- Name: salons_salon_id_seq; Type: SEQUENCE OWNED BY; Schema: core; Owner: shooperdooper
--

ALTER SEQUENCE core.salons_salon_id_seq OWNED BY core.salons.salon_id;


--
-- Name: schedules; Type: TABLE; Schema: core; Owner: shooperdooper
--

CREATE TABLE core.schedules (
    schedule_id integer NOT NULL,
    account_id integer,
    salon_id integer,
    release_schedule integer DEFAULT 28,
    frequency integer DEFAULT 1,
    outage boolean DEFAULT false,
    effective_start_date date NOT NULL,
    effective_end_date date
);


ALTER TABLE core.schedules OWNER TO shooperdooper;

--
-- Name: schedules_schedule_id_seq; Type: SEQUENCE; Schema: core; Owner: shooperdooper
--

CREATE SEQUENCE core.schedules_schedule_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE core.schedules_schedule_id_seq OWNER TO shooperdooper;

--
-- Name: schedules_schedule_id_seq; Type: SEQUENCE OWNED BY; Schema: core; Owner: shooperdooper
--

ALTER SEQUENCE core.schedules_schedule_id_seq OWNED BY core.schedules.schedule_id;


--
-- Name: services; Type: TABLE; Schema: core; Owner: shooperdooper
--

CREATE TABLE core.services (
    service_id integer NOT NULL,
    service_name character varying,
    service_duration integer,
    maximum_participants integer
);


ALTER TABLE core.services OWNER TO shooperdooper;

--
-- Name: services_service_id_seq; Type: SEQUENCE; Schema: core; Owner: shooperdooper
--

CREATE SEQUENCE core.services_service_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE core.services_service_id_seq OWNER TO shooperdooper;

--
-- Name: services_service_id_seq; Type: SEQUENCE OWNED BY; Schema: core; Owner: shooperdooper
--

ALTER SEQUENCE core.services_service_id_seq OWNED BY core.services.service_id;


--
-- Name: tech_services; Type: TABLE; Schema: core; Owner: shooperdooper
--

CREATE TABLE core.tech_services (
    tech_services_id integer NOT NULL,
    tech_id integer,
    service_id integer
);


ALTER TABLE core.tech_services OWNER TO shooperdooper;

--
-- Name: tech_services_tech_services_id_seq; Type: SEQUENCE; Schema: core; Owner: shooperdooper
--

CREATE SEQUENCE core.tech_services_tech_services_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE core.tech_services_tech_services_id_seq OWNER TO shooperdooper;

--
-- Name: tech_services_tech_services_id_seq; Type: SEQUENCE OWNED BY; Schema: core; Owner: shooperdooper
--

ALTER SEQUENCE core.tech_services_tech_services_id_seq OWNED BY core.tech_services.tech_services_id;


--
-- Name: accounts account_id; Type: DEFAULT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.accounts ALTER COLUMN account_id SET DEFAULT nextval('core.accounts_account_id_seq'::regclass);


--
-- Name: agreements id; Type: DEFAULT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.agreements ALTER COLUMN id SET DEFAULT nextval('core.agreements_agreement_id_seq'::regclass);


--
-- Name: day_time_range day_time_range_id; Type: DEFAULT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.day_time_range ALTER COLUMN day_time_range_id SET DEFAULT nextval('core.day_time_range_day_time_range_id_seq'::regclass);


--
-- Name: salons salon_id; Type: DEFAULT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.salons ALTER COLUMN salon_id SET DEFAULT nextval('core.salons_salon_id_seq'::regclass);


--
-- Name: schedules schedule_id; Type: DEFAULT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.schedules ALTER COLUMN schedule_id SET DEFAULT nextval('core.schedules_schedule_id_seq'::regclass);


--
-- Name: services service_id; Type: DEFAULT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.services ALTER COLUMN service_id SET DEFAULT nextval('core.services_service_id_seq'::regclass);


--
-- Name: tech_services tech_services_id; Type: DEFAULT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.tech_services ALTER COLUMN tech_services_id SET DEFAULT nextval('core.tech_services_tech_services_id_seq'::regclass);


--
-- Name: accounts accounts_pkey; Type: CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.accounts
    ADD CONSTRAINT accounts_pkey PRIMARY KEY (account_id);


--
-- Name: agreements agreements_pkey; Type: CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.agreements
    ADD CONSTRAINT agreements_pkey PRIMARY KEY (id);


--
-- Name: day_time_range day_time_range_pkey; Type: CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.day_time_range
    ADD CONSTRAINT day_time_range_pkey PRIMARY KEY (day_time_range_id);


--
-- Name: salons salons_pkey; Type: CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.salons
    ADD CONSTRAINT salons_pkey PRIMARY KEY (salon_id);


--
-- Name: schedules schedules_pkey; Type: CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.schedules
    ADD CONSTRAINT schedules_pkey PRIMARY KEY (schedule_id);


--
-- Name: services services_pkey; Type: CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.services
    ADD CONSTRAINT services_pkey PRIMARY KEY (service_id);


--
-- Name: tech_services tech_services_pkey; Type: CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.tech_services
    ADD CONSTRAINT tech_services_pkey PRIMARY KEY (tech_services_id);


--
-- Name: agreements agreements_salon_id_fkey; Type: FK CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.agreements
    ADD CONSTRAINT agreements_salon_id_fkey FOREIGN KEY (salon_id) REFERENCES core.salons(salon_id);


--
-- Name: schedules fk_account; Type: FK CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.schedules
    ADD CONSTRAINT fk_account FOREIGN KEY (account_id) REFERENCES core.accounts(account_id);


--
-- Name: agreements fk_client_id; Type: FK CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.agreements
    ADD CONSTRAINT fk_client_id FOREIGN KEY (tech_id) REFERENCES core.accounts(account_id);


--
-- Name: schedules fk_salon; Type: FK CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.schedules
    ADD CONSTRAINT fk_salon FOREIGN KEY (salon_id) REFERENCES core.salons(salon_id);


--
-- Name: day_time_range fk_schedule; Type: FK CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.day_time_range
    ADD CONSTRAINT fk_schedule FOREIGN KEY (schedule_id) REFERENCES core.schedules(schedule_id);


--
-- Name: agreements fk_service_id; Type: FK CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.agreements
    ADD CONSTRAINT fk_service_id FOREIGN KEY (service_id) REFERENCES core.services(service_id);


--
-- Name: tech_services fk_service_id; Type: FK CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.tech_services
    ADD CONSTRAINT fk_service_id FOREIGN KEY (service_id) REFERENCES core.services(service_id);


--
-- Name: agreements fk_tech_id; Type: FK CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.agreements
    ADD CONSTRAINT fk_tech_id FOREIGN KEY (tech_id) REFERENCES core.accounts(account_id);


--
-- Name: tech_services fk_tech_id; Type: FK CONSTRAINT; Schema: core; Owner: shooperdooper
--

ALTER TABLE ONLY core.tech_services
    ADD CONSTRAINT fk_tech_id FOREIGN KEY (tech_id) REFERENCES core.accounts(account_id);


--
-- PostgreSQL database dump complete
--

\unrestrict 3rh0D2EeDdrgdm62ig3FhUpZtsz1uzN8det1AiqgAbYJBoMLBVv0qgE8FFU9EKB

