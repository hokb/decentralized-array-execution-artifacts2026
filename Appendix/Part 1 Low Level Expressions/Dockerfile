# Build stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0  
WORKDIR /Artefact1
COPY FORTRAN ./FORTRAN/

COPY numpy ./numpy/
COPY ILNumerics/*.* ./ILNumerics/
COPY ILNumerics/publish ./ILNumerics/publish/
COPY ILNumerics/Properties ./ILNumerics/Properties/
COPY ILNumerics/ILNumerics_deploy ./ILNumerics/ILNumerics_deploy/

#WORKDIR /Artefact1/ILNumerics
#
#RUN dotnet restore Part1.csproj
#RUN dotnet publish -c Release -o /Artefact1/ILNumerics/publish/Release/net8.0/ --no-restore
#
RUN apt-get update && \
    apt-get install -y --no-install-recommends gfortran python3 python3-pip python3-venv build-essential libgdiplus && \
    rm -rf /var/lib/apt/lists/*

RUN python3 -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

RUN /opt/venv/bin/pip install --upgrade pip && \
    /opt/venv/bin/pip install numpy numba

WORKDIR /Artefact1/FORTRAN
RUN gfortran -O3 Part1.f90 -o Part1_F90_optim -cpp -DOPTIM
RUN gfortran -O0 Part1.f90 -o Part1_F90_nooptim -cpp 

WORKDIR /Artefact1/ILNumerics/publish/Release/net8.0/

ENTRYPOINT ["dotnet", "Part1.dll"]