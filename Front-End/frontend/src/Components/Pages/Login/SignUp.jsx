import React, { useState } from 'react';
import axios from 'axios';
import Cookies from 'js-cookie';
import LogoLogin from "../assets/logoLogin.png";
import { useNavigate } from 'react-router-dom';
import { Eye, EyeSlash } from "@phosphor-icons/react";

const SignIn = ({ onSignUpClick }) => {
    const [name, setName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const navigate = useNavigate();
    const [showPassword, setShowPassword] = useState(false); // Para controlar visibilidade da senha

    const handSubmit = async (event) => {
        event.preventDefault();

        try {
            const response = await axios.post('https://localhost:7266/api/User/Create', {
                inageUser: 'guigas',
                nameUser: name, 
                emailUser: email,
                passwordUser: password,
                phoneUser: '(17)99999-9999',
            });

            if (response.status === 200) {
                Elevator('Usuário cadastrado com sucesso!');
            }
        } catch (error) {
            setError('Falha ao cadastrar o usuário. Verifique os dados e tente novamente.');
        }
            };
}