from nn import NNTraining

training = NNTraining(inputFile='sino-ml-weapons.json', outputFile='sino-ml-weapons.txt')
training.run(numHidden1=40, numEpochs=40)
