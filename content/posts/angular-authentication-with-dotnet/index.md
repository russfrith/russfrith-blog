---
title: "Angular Authentication With .NET"
date: 2022-05-09T11:30:40-04:00
draft: true
cover:
  image: pexels-gotta-be-worth-it-1160845.jpg
  alt: "Assorted-color metal locks"
  caption: "Photo by [Gotta Be Worth It](https://www.pexels.com/@myersmc16/) from [Pexels](https://www.pexels.com/photo/assorted-color-metal-keys-1160845/)"
tags: ["angular", "dotnet", "csharp", "oauth", "jwt"]
categories: ["programming"]
---

## Authentication vs Authorization
Authentication is the act of validating that a user is whom they claim to be. All other security measures are based on the premise that the system has properly identified the current user. Authorization is the act of granting an authenticated party permission to access a specific resource or function. 

Username and password, also known as password authentication, is the most popular authentication mechanism. Password authentication is fraught with challenges. Users typically choose weak passwords and repeat them among multiple websites.

### JWT
JSON Web Tokens (JWT) are an open, URL-safe, industry standard [RFC 7519](https://www.rfc-editor.org/rfc/rfc7519) method for representing claims securely between two parties.